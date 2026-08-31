namespace Nextended.Aspire.Hosting.DbTools;

/// PostgreSQL, cloned with the tools inside its own image.
///
/// `pg_dump | psql` rather than a custom copy: the dump carries the schema, the data, the indexes,
/// the constraints, the sequences and their current values — everything a hand-written copy forgets.
///
/// One version rule worth knowing: `pg_dump` refuses a server newer than itself, so the image the
/// clone runs in has to be at least the version of the source. It defaults to the target's own
/// image, which is usually the same or newer; `DbCloneOptions.Image` is there for when it is not.
internal static class PostgresCloneRecipe
{
    internal const int DefaultPort = 5432;
    internal const string DefaultImage = "postgres";
    internal const string DefaultTag = "17-alpine";

    internal static string Script() => """
        set -e

        export PGPASSWORD="$CLONE_TARGET_PASSWORD"
        target="-h $CLONE_TARGET_HOST -p $CLONE_TARGET_PORT -U $CLONE_TARGET_USER"

        # The target is a container in this stack and may still be starting.
        echo "waiting for $CLONE_TARGET_HOST:$CLONE_TARGET_PORT"
        until pg_isready $target -d "$CLONE_TARGET_DB" >/dev/null 2>&1; do sleep 1; done

        # And so may the source, when it is one of ours.
        export PGPASSWORD="$CLONE_SOURCE_PASSWORD"
        source_args="-h $CLONE_SOURCE_HOST -p $CLONE_SOURCE_PORT -U $CLONE_SOURCE_USER"

        echo "waiting for $CLONE_SOURCE_HOST:$CLONE_SOURCE_PORT"
        until pg_isready $source_args -d "$CLONE_SOURCE_DB" >/dev/null 2>&1; do sleep 1; done

        if [ "$CLONE_ONLY_WHEN_EMPTY" = "1" ]; then
          export PGPASSWORD="$CLONE_TARGET_PASSWORD"

          tables=$(psql $target -d "$CLONE_TARGET_DB" -tAc \
            "select count(*) from information_schema.tables where table_schema not in ('pg_catalog','information_schema')")

          if [ "${tables:-0}" -gt 0 ]; then
            echo "$CLONE_TARGET_DB already has $tables table(s); nothing was copied"
            exit 0
          fi
        fi

        # --no-owner and --no-privileges: the roles of the source do not exist here, and a restore
        # that fails on GRANT statements for a role nobody has is a restore nobody wanted.
        dump="--no-owner --no-privileges"
        [ "$CLONE_SCHEMA_ONLY" = "1" ] && dump="$dump --schema-only"
        [ "$CLONE_DATA_ONLY" = "1" ] && dump="$dump --data-only"

        # --clean --if-exists drops what it is about to replace, which is what overwriting means.
        [ "$CLONE_OVERWRITE" = "1" ] && dump="$dump --clean --if-exists"

        echo "cloning $CLONE_SOURCE_DB into $CLONE_TARGET_DB"

        # One pipe, no temporary file: a dump of a large database is larger than the container's
        # disk deserves, and psql reads a stream as happily as a file.
        #
        # ON_ERROR_STOP so a broken restore is a failure rather than a half-full database with a
        # zero exit code. PGPASSWORD is per side, so each half of the pipe gets its own.
        if PGPASSWORD="$CLONE_SOURCE_PASSWORD" pg_dump $source_args -d "$CLONE_SOURCE_DB" $dump \
           | PGPASSWORD="$CLONE_TARGET_PASSWORD" psql $target -d "$CLONE_TARGET_DB" \
               -v ON_ERROR_STOP=1 --quiet -o /dev/null
        then
          export PGPASSWORD="$CLONE_TARGET_PASSWORD"
          echo "done: $(psql $target -d "$CLONE_TARGET_DB" -tAc \
            "select count(*) from information_schema.tables where table_schema not in ('pg_catalog','information_schema')") table(s) in $CLONE_TARGET_DB"
        else
          echo "the clone failed"
          exit 1
        fi
        """;
}
