namespace Nextended.Aspire.Hosting.DbTools;

/// MySQL and MariaDB, cloned with the tools inside their own image.
///
/// `mysqldump | mysql`, which carries the schema, the rows, the indexes, the foreign keys, the
/// triggers and the auto-increment counters. Routines and events are asked for explicitly, because
/// mysqldump leaves them out by default and a clone that quietly dropped every stored procedure
/// would be a poor clone.
internal static class MySqlCloneRecipe
{
    internal const int DefaultPort = 3306;
    internal const string DefaultImage = "mysql";
    internal const string DefaultTag = "8.4";

    internal static string Script() => """
        set -e

        source_args="-h $CLONE_SOURCE_HOST -P $CLONE_SOURCE_PORT -u $CLONE_SOURCE_USER"
        target="-h $CLONE_TARGET_HOST -P $CLONE_TARGET_PORT -u $CLONE_TARGET_USER"

        echo "waiting for $CLONE_TARGET_HOST:$CLONE_TARGET_PORT"
        until MYSQL_PWD="$CLONE_TARGET_PASSWORD" mysql $target -e "select 1" >/dev/null 2>&1; do sleep 1; done

        echo "waiting for $CLONE_SOURCE_HOST:$CLONE_SOURCE_PORT"
        until MYSQL_PWD="$CLONE_SOURCE_PASSWORD" mysql $source_args -e "select 1" >/dev/null 2>&1; do sleep 1; done

        count_tables() {
          MYSQL_PWD="$CLONE_TARGET_PASSWORD" mysql $target -N -B -e \
            "select count(*) from information_schema.tables where table_schema = '$CLONE_TARGET_DB'"
        }

        if [ "$CLONE_ONLY_WHEN_EMPTY" = "1" ]; then
          tables=$(count_tables)

          if [ "${tables:-0}" -gt 0 ]; then
            echo "$CLONE_TARGET_DB already has $tables table(s); nothing was copied"
            exit 0
          fi
        fi

        # The database itself may not exist yet when the target is a bare server.
        MYSQL_PWD="$CLONE_TARGET_PASSWORD" mysql $target \
          -e "CREATE DATABASE IF NOT EXISTS \`$CLONE_TARGET_DB\`"

        # --routines and --events: left out by default, and a clone without the stored procedures is
        # not the same database. --single-transaction reads a consistent picture without locking the
        # source, which matters when the source is something somebody else is using.
        dump="--single-transaction --routines --events --triggers --no-tablespaces"
        [ "$CLONE_SCHEMA_ONLY" = "1" ] && dump="$dump --no-data"
        [ "$CLONE_DATA_ONLY" = "1" ] && dump="$dump --no-create-info --skip-triggers"
        [ "$CLONE_OVERWRITE" = "1" ] && dump="$dump --add-drop-table"

        echo "cloning $CLONE_SOURCE_DB into $CLONE_TARGET_DB"

        if MYSQL_PWD="$CLONE_SOURCE_PASSWORD" mysqldump $source_args $dump "$CLONE_SOURCE_DB" \
           | MYSQL_PWD="$CLONE_TARGET_PASSWORD" mysql $target "$CLONE_TARGET_DB"
        then
          echo "done: $(count_tables) table(s) in $CLONE_TARGET_DB"
        else
          echo "the clone failed"
          exit 1
        fi
        """;
}
