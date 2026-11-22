#!/bin/sh
# Creates the dashboards and alert rules of this folder once SigNoz answers. Both are created when missing and left alone
# otherwise, so changes made in the UI survive a restart; delete one in the UI to get the shipped version back.
# BusyBox only (the SigNoz image is Alpine without curl or jq): wget, sed, grep.
dir=$(dirname "$0")
api=http://127.0.0.1:8080
log() { echo "provisioning: $*"; }

until wget -q -O /dev/null "$api/api/v1/health" 2>/dev/null; do sleep 2; done

token=""
if [ "$SIGNOZ_IDENTN_IMPERSONATION_ENABLED" != "true" ]; then
  esc() { printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g'; }
  email=$(printf '%s' "$SIGNOZ_USER_ROOT_EMAIL" | sed -e 's/+/%2B/g')
  # the root user is created by SigNoz itself shortly after it answers
  for _ in $(seq 1 60); do
    org=$(wget -q -O - "$api/api/v2/sessions/context?email=$email&ref=$api" 2>/dev/null | sed -n 's/.*"orgs":\[{"id":"\([^"]*\)".*/\1/p')
    [ -n "$org" ] && break
    sleep 2
  done
  body="{\"email\":\"$(esc "$SIGNOZ_USER_ROOT_EMAIL")\",\"password\":\"$(esc "$SIGNOZ_USER_ROOT_PASSWORD")\",\"orgId\":\"$org\"}"
  token=$(wget -q -O - --header "Content-Type: application/json" --post-data "$body" "$api/api/v2/sessions/email_password" 2>/dev/null \
    | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')
  if [ -z "$token" ]; then
    log "cannot sign in as $SIGNOZ_USER_ROOT_EMAIL, nothing provisioned"
    exit 0
  fi
fi

# get <path> / post <path> <file>: with the token when there is one
get() {
  if [ -n "$token" ]; then wget -q -O - --header "Authorization: Bearer $token" "$api$1"; else wget -q -O - "$api$1"; fi
}
post() {
  if [ -n "$token" ]; then
    wget -q -O /dev/null --header "Authorization: Bearer $token" --header "Content-Type: application/json" --post-file "$2" "$api$1" 2>&1
  else
    wget -q -O /dev/null --header "Content-Type: application/json" --post-file "$2" "$api$1" 2>&1
  fi
}

for file in "$dir"/dashboards/*.json; do
  [ -f "$file" ] || continue
  # a dashboard's name is unique: 409 means it is there already
  result=$(post /api/v2/dashboards "$file")
  case "$result" in
    "") log "dashboard $(basename "$file") created" ;;
    *409*) ;;
    *) log "dashboard $(basename "$file") refused: $result" ;;
  esac
done

rules=$(get "/api/v3/rules?limit=1000" 2>/dev/null)
for file in "$dir"/alerts/*.json; do
  [ -f "$file" ] || continue
  name=$(sed -n 's/^ *"alert": *"\([^"]*\)".*/\1/p' "$file" | head -n 1)
  case "$rules" in
    *"\"alert\":\"$name\""*) continue ;;
  esac
  result=$(post /api/v2/rules "$file")
  if [ -z "$result" ]; then log "alert rule $name created"; else log "alert rule $name refused: $result"; fi
done
log "done"
