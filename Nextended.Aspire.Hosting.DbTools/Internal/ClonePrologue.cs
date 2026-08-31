namespace Nextended.Aspire.Hosting.DbTools;

/// Takes a connection string apart inside the container.
///
/// Where the parts are known when the app host is built, they are passed as five variables and this
/// does nothing. Where the source is a parameter or an `AddConnectionString` resource, the value is
/// not known until the stack runs — so the whole string arrives as one variable, and the same two
/// forms have to be understood here that <see cref="DbEndpoint.Parse"/> understands in C#: the
/// ADO.NET one and the URI one.
///
/// POSIX shell and one small awk, because that is the intersection of what these five images have:
/// two of them are Alpine with busybox, the others are Debian, Ubuntu and Oracle Linux.
internal static class ClonePrologue
{
    /// Prepended to every recipe. Ends with the five CLONE_SOURCE_* and CLONE_TARGET_* variables
    /// set, whichever way they arrived.
    internal static string Script(int defaultPort) => $$"""
        # --- a connection string, as five variables ----------------------------------------------

        # One field of the ADO.NET form. `names` is a |-separated list, because the same field is
        # spelled three ways depending on whose documentation somebody had open, and two of those
        # spellings contain a space.
        _field() {
            printf '%s\n' "$1" | tr ';' '\n' | awk -v names="$2" '
                {
                    line = $0
                    key = line
                    sub(/=.*/, "", key)
                    gsub(/^[ \t]+|[ \t]+$/, "", key)
                    key = tolower(key)

                    count = split(names, wanted, "|")

                    for (i = 1; i <= count; i++) {
                        if (key == wanted[i]) {
                            sub(/^[^=]*=/, "", line)
                            gsub(/^[ \t]+|[ \t]+$/, "", line)

                            # A value may be quoted, and has to be when it holds a semicolon or a
                            # space - Azure writes the password quoted. The quotes belong to the
                            # format, not to the password. 39 and 34 are the two quote characters,
                            # written as codes so this program needs no quoting of its own.
                            if (length(line) > 1) {
                                q = substr(line, 1, 1)

                                if ((q == sprintf("%c", 39) || q == sprintf("%c", 34)) && substr(line, length(line), 1) == q) {
                                    line = substr(line, 2, length(line) - 2)
                                }
                            }

                            print line
                            exit
                        }
                    }
                }'
        }

        # $1 = the whole string, $2 = the prefix to export into.
        _split() {
            _whole="$1"; _prefix="$2"
            _host=""; _port=""; _user=""; _password=""; _database=""

            case "$_whole" in
                *://*)
                    # scheme://user:password@host:port/database?options
                    _rest=${_whole#*://}
                    _creds=""

                    case "$_rest" in
                        *@*) _creds=${_rest%%@*}; _rest=${_rest#*@} ;;
                    esac

                    _hostport=${_rest%%/*}

                    case "$_rest" in
                        */*) _database=${_rest#*/}; _database=${_database%%\?*} ;;
                    esac

                    _host=${_hostport%%:*}
                    case "$_hostport" in
                        *:*) _port=${_hostport##*:} ;;
                    esac

                    if [ -n "$_creds" ]; then
                        _user=${_creds%%:*}
                        case "$_creds" in
                            *:*) _password=${_creds#*:} ;;
                        esac
                    fi
                    ;;
                *)
                    _host=$(_field "$_whole" "host|server|data source|address|addr|network address")
                    _port=$(_field "$_whole" "port")
                    _user=$(_field "$_whole" "username|user id|userid|uid|user")
                    _password=$(_field "$_whole" "password|pwd")
                    _database=$(_field "$_whole" "database|initial catalog|db")

                    # "Server=localhost,1433" is how SQL Server writes a port.
                    case "$_host" in
                        *,*) _port=${_host##*,}; _host=${_host%%,*} ;;
                    esac

                    # "Server=tcp:host" is how Azure writes a host. The protocol prefix is SQL
                    # Server's own spelling and means nothing to any other tool, so it goes.
                    case "$_host" in
                        tcp:*|np:*|lpc:*) _host=${_host#*:} ;;
                    esac
                    ;;
            esac

            [ -z "$_port" ] && _port={{defaultPort}}

            export "${_prefix}HOST=$_host"
            export "${_prefix}PORT=$_port"
            export "${_prefix}USER=$_user"
            export "${_prefix}PASSWORD=$_password"
            export "${_prefix}DB=$_database"
        }

        [ -n "$CLONE_SOURCE_URL" ] && _split "$CLONE_SOURCE_URL" "CLONE_SOURCE_"
        [ -n "$CLONE_TARGET_URL" ] && _split "$CLONE_TARGET_URL" "CLONE_TARGET_"

        # --- the recipe --------------------------------------------------------------------------

        """;
}
