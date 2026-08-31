namespace Nextended.Aspire.Hosting.DbTools;

/// MongoDB, cloned with the tools inside its own image.
///
/// `mongodump --archive | mongorestore --archive`: one stream, no temporary file, and the archive
/// carries the collections, their documents and their indexes.
///
/// A database here is a namespace rather than a schema, so "empty" means "has no collections" —
/// which `mongosh` can answer in one line, and which it is, since mongosh is in the image too.
internal static class MongoCloneRecipe
{
    internal const int DefaultPort = 27017;
    internal const string DefaultImage = "mongo";
    internal const string DefaultTag = "8";

    internal static string Script() => """
        set -e

        # A URI rather than flags: Mongo's own tools take one, and it is the form a connection string
        # already has. Credentials are optional — a development server often has none.
        uri() {
          host="$1"; port="$2"; user="$3"; password="$4"; database="$5"

          if [ -n "$user" ]; then
            echo "mongodb://$user:$password@$host:$port/$database?authSource=admin"
          else
            echo "mongodb://$host:$port/$database"
          fi
        }

        source_uri=$(uri "$CLONE_SOURCE_HOST" "$CLONE_SOURCE_PORT" "$CLONE_SOURCE_USER" \
                         "$CLONE_SOURCE_PASSWORD" "$CLONE_SOURCE_DB")
        target_uri=$(uri "$CLONE_TARGET_HOST" "$CLONE_TARGET_PORT" "$CLONE_TARGET_USER" \
                         "$CLONE_TARGET_PASSWORD" "$CLONE_TARGET_DB")

        echo "waiting for $CLONE_TARGET_HOST:$CLONE_TARGET_PORT"
        until mongosh "$target_uri" --quiet --eval "db.runCommand({ping:1})" >/dev/null 2>&1; do sleep 1; done

        echo "waiting for $CLONE_SOURCE_HOST:$CLONE_SOURCE_PORT"
        until mongosh "$source_uri" --quiet --eval "db.runCommand({ping:1})" >/dev/null 2>&1; do sleep 1; done

        # There is no schema to copy on its own here: a collection is its documents. Asking for the
        # shape without them is asking for nothing, and saying so beats a silent empty database.
        if [ "$CLONE_SCHEMA_ONLY" = "1" ]; then
          echo "MongoDB has no schema apart from its documents; a schema-only clone would be empty"
          exit 1
        fi

        count_collections() {
          mongosh "$target_uri" --quiet --eval "db.getCollectionNames().length"
        }

        if [ "$CLONE_ONLY_WHEN_EMPTY" = "1" ]; then
          collections=$(count_collections)

          if [ "${collections:-0}" -gt 0 ]; then
            echo "$CLONE_TARGET_DB already has $collections collection(s); nothing was copied"
            exit 0
          fi
        fi

        # --drop replaces each collection as it arrives rather than merging into it, which is what
        # replacing a database means. Without it a restore adds to what is there.
        restore=""
        [ "$CLONE_OVERWRITE" = "1" ] && restore="--drop"

        echo "cloning $CLONE_SOURCE_DB into $CLONE_TARGET_DB"

        if mongodump --uri="$source_uri" --archive \
           | mongorestore --uri="$target_uri" --archive $restore \
               --nsFrom="$CLONE_SOURCE_DB.*" --nsTo="$CLONE_TARGET_DB.*"
        then
          echo "done: $(count_collections) collection(s) in $CLONE_TARGET_DB"
        else
          echo "the clone failed"
          exit 1
        fi
        """;
}
