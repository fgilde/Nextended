#!/bin/sh
# Provisioning runs in the background; SigNoz replaces this shell so it keeps PID 1 and gets the stop signal itself.
sh "$(dirname "$0")/provision.sh" &
cd /root
exec ./signoz server
