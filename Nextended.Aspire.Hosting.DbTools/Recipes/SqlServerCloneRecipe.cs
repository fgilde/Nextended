namespace Nextended.Aspire.Hosting.DbTools;

/// SQL Server, cloned as a BACPAC.
///
/// The other engines keep their dump tools inside their own image; SQL Server's are split. `sqlcmd`
/// and `bcp` are in the server image and neither carries a schema; `sqlpackage`, which does, is a
/// .NET tool that is in no image at all. So the clone runs in the .NET SDK image and installs it —
/// which means a clone needs the internet the first time it runs, and an air-gapped stack points
/// <see cref="DbCloneOptions.Image"/> at an image with sqlpackage already in it.
///
/// It also means the .NET *8* SDK: sqlpackage 170 runs on net8, and the 9 image has no 8 runtime.
///
/// Two things fall out of BACPAC that are worth knowing. It goes server to server over the network,
/// so an external source works exactly like one in the stack. And an import into a database that
/// already holds objects is refused by the tool itself, with SQL71659 — which is the "only when
/// empty" rule, enforced without asking anybody.
internal static class SqlServerCloneRecipe
{
    internal const int DefaultPort = 1433;
    internal const string DefaultImage = "mcr.microsoft.com/dotnet/sdk";
    internal const string DefaultTag = "8.0";

    /// The container that clears the way, for the one case the tool cannot: replacing a database
    /// that is already there. It runs in the server's own image, because `sqlcmd` lives there.
    internal const string PrepareImage = "mcr.microsoft.com/mssql/server";
    internal const string PrepareTag = "2022-latest";

    internal static string PrepareScript() => """
        set -e

        sqlcmd="/opt/mssql-tools18/bin/sqlcmd -C -S $CLONE_TARGET_HOST,$CLONE_TARGET_PORT"
        sqlcmd="$sqlcmd -U $CLONE_TARGET_USER -P $CLONE_TARGET_PASSWORD"

        echo "waiting for $CLONE_TARGET_HOST:$CLONE_TARGET_PORT"
        until $sqlcmd -Q "select 1" >/dev/null 2>&1; do sleep 2; done

        # SINGLE_USER first: a database with a session on it cannot be dropped, and the studio or an
        # application may well have opened one already.
        echo "dropping $CLONE_TARGET_DB so the clone can replace it"
        $sqlcmd -Q "IF DB_ID('$CLONE_TARGET_DB') IS NOT NULL BEGIN
                      ALTER DATABASE [$CLONE_TARGET_DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                      DROP DATABASE [$CLONE_TARGET_DB];
                    END"

        echo "done"
        """;

    internal static string Script() => """
        set -e
        export PATH="$PATH:/root/.dotnet/tools"

        echo "installing sqlpackage"
        dotnet tool install -g microsoft.sqlpackage >/dev/null

        # Encrypt is on by default in the modern drivers, and a development server's certificate is
        # self-signed: without this every connection fails on a certificate nobody can fix.
        common="TrustServerCertificate=True"

        source_cs="Server=$CLONE_SOURCE_HOST,$CLONE_SOURCE_PORT;Database=$CLONE_SOURCE_DB"
        source_cs="$source_cs;User Id=$CLONE_SOURCE_USER;Password=$CLONE_SOURCE_PASSWORD;$common"

        target_cs="Server=$CLONE_TARGET_HOST,$CLONE_TARGET_PORT;Database=$CLONE_TARGET_DB"
        target_cs="$target_cs;User Id=$CLONE_TARGET_USER;Password=$CLONE_TARGET_PASSWORD;$common"

        # No client here to ask "are you up yet" with, so the export is the question: it fails while
        # the source is still starting, and a stack's databases take their time.
        echo "exporting $CLONE_SOURCE_DB"
        exported=0

        for attempt in $(seq 1 60); do
          if sqlpackage /a:Export /scs:"$source_cs" /tf:/tmp/clone.bacpac /q:True >/tmp/export.log 2>&1; then
            exported=1
            break
          fi

          sleep 5
        done

        if [ "$exported" != "1" ]; then
          echo "the source could not be exported:"
          tail -5 /tmp/export.log
          exit 1
        fi

        echo "exported $(wc -c < /tmp/clone.bacpac) byte(s)"

        # Import creates the database. Into one that already holds objects it refuses with SQL71659,
        # which is exactly the rule we want — so that refusal is read rather than fought.
        echo "importing into $CLONE_TARGET_DB"

        for attempt in $(seq 1 60); do
          if sqlpackage /a:Import /tcs:"$target_cs" /sf:/tmp/clone.bacpac /q:True >/tmp/import.log 2>&1; then
            echo "done: $CLONE_TARGET_DB"
            exit 0
          fi

          if grep -q "SQL71659" /tmp/import.log; then
            if [ "$CLONE_ONLY_WHEN_EMPTY" = "1" ]; then
              echo "$CLONE_TARGET_DB already has objects in it; nothing was copied"
              exit 0
            fi

            echo "$CLONE_TARGET_DB already has objects in it, and this clone was told to replace it"
            tail -3 /tmp/import.log
            exit 1
          fi

          # Anything else is either a target that is not up yet, or a real failure; both are worth
          # another try, and the loop is what decides which it was.
          sleep 5
        done

        echo "the import did not succeed:"
        tail -5 /tmp/import.log
        exit 1
        """;
}
