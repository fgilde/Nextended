namespace Nextended.Aspire.Hosting.DbTools;

/// Redis, cloned by letting Redis do it.
///
/// The other engines dump and restore. Redis already knows how to copy a whole server to another
/// one — it is what a replica is — so the clone points the target at the source, waits for the sync
/// to finish, and then cuts it loose again. That transfers every key of every type, with its TTL,
/// in the binary form Redis itself uses; a loop over `DUMP` and `RESTORE` in a shell would be
/// slower and would have to survive binary payloads in shell variables.
///
/// Two things about the server Aspire starts that a plain `redis-cli` gets wrong. It wants a
/// password, and TLS is on its usual port, with the plaintext one next to it — so the script finds
/// out which one answers before it does anything. Replication over TLS also has to be turned on for
/// the duration, which is a `CONFIG SET` on the target and is put back afterwards.
///
/// A clone replaces the whole target: Redis has no database to scope this to, and a replica is not
/// a merge.
internal static class RedisCloneRecipe
{
    internal const int DefaultPort = 6379;
    internal const string DefaultImage = "redis";
    internal const string DefaultTag = "8-alpine";

    internal static string Script() => """
        set -e

        # Which way each end answers: TLS on the usual port, or plaintext next door. Asked rather
        # than assumed, because both are configurations somebody deploys on purpose.
        #
        # Every attempt is wrapped in `timeout`, and that is not the same as redis-cli's own -t:
        # its timeout covers connecting, not the TLS handshake. A TLS client against a plaintext
        # port connects fine and then waits for a handshake that is never coming — measured, and it
        # hangs for as long as anybody is willing to watch.
        find_cli() {
            host="$1"; port="$2"; password="$3"
            auth=""
            [ -n "$password" ] && auth="-a $password --no-auth-warning"

            plain=$((port + 1))

            for candidate in "--tls --insecure -h $host -p $port" "-h $host -p $port" "-h $host -p $plain"; do
                # The whole attempt's noise goes away, including `timeout`'s own word for
                # having stopped one: a probe that did not answer is not news.
                if { timeout 3 redis-cli $candidate $auth ping; } 2>/dev/null | grep -q PONG; then
                    echo "$candidate $auth"
                    return 0
                fi
            done

            return 1
        }

        wait_for() {
            for attempt in $(seq 1 120); do
                if answer=$(find_cli "$1" "$2" "$3"); then
                    echo "$answer"
                    return 0
                fi
                sleep 1
            done

            return 1
        }

        echo "waiting for $CLONE_TARGET_HOST:$CLONE_TARGET_PORT"
        target=$(wait_for "$CLONE_TARGET_HOST" "$CLONE_TARGET_PORT" "$CLONE_TARGET_PASSWORD") || {
            echo "the target never answered on either port"
            exit 1
        }

        echo "waiting for $CLONE_SOURCE_HOST:$CLONE_SOURCE_PORT"
        source_cli=$(wait_for "$CLONE_SOURCE_HOST" "$CLONE_SOURCE_PORT" "$CLONE_SOURCE_PASSWORD") || {
            echo "the source never answered on either port"
            exit 1
        }

        if [ "$CLONE_SCHEMA_ONLY" = "1" ]; then
          echo "Redis has no schema apart from its keys; a schema-only clone would be empty"
          exit 1
        fi

        keys=$(redis-cli $target DBSIZE | tr -dc '0-9')

        if [ "$CLONE_ONLY_WHEN_EMPTY" = "1" ] && [ "${keys:-0}" -gt 0 ]; then
          echo "the target already holds $keys key(s); nothing was copied"
          exit 0
        fi

        # Replication carries the source's own view of its data, and the target's own port tells the
        # source where to send it. The source's *internal* port matters here, not the one the clone
        # reached it on: a replica connects the same way a client does.
        source_host="$CLONE_SOURCE_HOST"
        source_port=$(echo "$source_cli" | sed -n 's/.*-p \([0-9]*\).*/\1/p')

        echo "##progress 20 copying"
        echo "replicating $source_host:$source_port onto the target"

        # Over TLS a replica needs telling; harmless where there is none, and put back below.
        case "$target" in
          *--tls*) redis-cli $target CONFIG SET tls-replication yes >/dev/null 2>&1 || true ;;
        esac

        [ -n "$CLONE_SOURCE_PASSWORD" ] && \
          redis-cli $target CONFIG SET masterauth "$CLONE_SOURCE_PASSWORD" >/dev/null 2>&1 || true

        redis-cli $target REPLICAOF "$source_host" "$source_port" >/dev/null

        synced=0

        for attempt in $(seq 1 "${CLONE_TIMEOUT:-600}"); do
          state=$(redis-cli $target INFO replication 2>/dev/null | tr -d '\r')

          case "$state" in
            *master_link_status:up*)
              case "$state" in
                *master_sync_in_progress:0*) synced=1 ;;
              esac
              ;;
          esac

          [ "$synced" = "1" ] && break
          sleep 1
        done

        # Loose again either way: a target left as somebody's replica is read-only for ever, which
        # is a far worse outcome than a clone that did not finish.
        redis-cli $target REPLICAOF NO ONE >/dev/null

        if [ "$synced" != "1" ]; then
          echo "the target never finished syncing from the source"
          exit 1
        fi

        echo "##progress 100 Cloned"
          echo "done: $(redis-cli $target DBSIZE | tr -dc '0-9') key(s) on the target"
        """;
}
