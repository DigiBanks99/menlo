#!/usr/bin/env bash
# Creates or deletes the short-lived Entra ID client secret that the backend CI job tests with.
#
#   ephemeral-aad-secret.sh create <app-id> <display-name>   # writes client-secret to $GITHUB_OUTPUT
#   ephemeral-aad-secret.sh delete <app-id> <display-name>   # deletes every credential with that display name
#
# Entra ID throttles the tenant now and then ("concurrent requests being made to the tenant"), so each
# az call is retried with backoff on transient errors only. `credential reset --append` isn't idempotent,
# so before retrying a reset any credential that already has the display name is deleted: it comes from
# the attempt whose response was lost, and its password can't be recovered. Graph listings can lag a
# write, so a stray one can still slip through; the workflow's cleanup step (`delete`) removes every
# credential with the display name, so none outlive the run.
#
# AZ_RETRY_DELAYS overrides the backoff, in seconds between attempts (default "10 20", so 3 attempts).
set -uo pipefail

read -r -a retry_delays <<<"${AZ_RETRY_DELAYS:-10 20}"
max_attempts=$((${#retry_delays[@]} + 1))

transient_pattern='concurrent requests|throttl|Too ?Many ?Requests|Service ?Unavailable|Bad ?Gateway|Gateway ?Time-?out|timed out|temporarily unavailable|Connection (reset|aborted|refused)'
# A delete retried after a throttled attempt that went through finds nothing to delete.
gone_pattern='does not exist|not found|NotFound'

# is_transient <stderr-file>
is_transient() {
    grep -qiE "$transient_pattern" "$1"
}

# backoff <attempt>: sleeps before the next attempt
backoff() {
    local delay="${retry_delays[$(($1 - 1))]}"
    echo "Transient error, retrying in ${delay}s (attempt $(($1 + 1))/$max_attempts)" >&2
    sleep "$delay"
}

# az_retry <az args...>: prints az's stdout; retries transient failures, fails fast on the rest.
az_retry() {
    local attempt=1 out status err
    err="$(mktemp)"
    while :; do
        out="$(az "$@" 2>"$err")"
        status=$?
        if [ "$status" -eq 0 ]; then
            rm -f "$err"
            printf '%s' "$out"
            return 0
        fi
        cat "$err" >&2
        if [ "$attempt" -ge "$max_attempts" ] || ! is_transient "$err"; then
            rm -f "$err"
            return "$status"
        fi
        backoff "$attempt"
        attempt=$((attempt + 1))
    done
}

# key_ids <app-id> <display-name>: one keyId per line
key_ids() {
    az_retry ad app credential list --id "$1" --query "[?displayName=='$2'].keyId" --output tsv
}

# delete_one <app-id> <key-id>: a credential that is already gone counts as deleted
delete_one() {
    local err status
    err="$(mktemp)"
    az_retry ad app credential delete --id "$1" --key-id "$2" 2>"$err"
    status=$?
    cat "$err" >&2
    if [ "$status" -ne 0 ] && grep -qiE "$gone_pattern" "$err"; then
        status=0
    fi
    rm -f "$err"
    return "$status"
}

# delete_all <app-id> <display-name>: prints one line per deleted credential
delete_all() {
    local ids id
    ids="$(key_ids "$1" "$2")" || return 1
    for id in $ids; do
        delete_one "$1" "$id" || return 1
        echo "Deleted credential $id ($2)"
    done
}

create() {
    local app_id="$1" name="$2" attempt=1 secret status err end_date
    err="$(mktemp)"
    while :; do
        if [ "$attempt" -gt 1 ]; then
            delete_all "$app_id" "$name" || { rm -f "$err"; return 1; }
        fi
        end_date="$(date -u -d '+1 hour' '+%Y-%m-%dT%H:%M:%S+00:00')"
        secret="$(az ad app credential reset \
            --id "$app_id" \
            --display-name "$name" \
            --end-date "$end_date" \
            --append \
            --query password \
            --output tsv 2>"$err")"
        status=$?
        if [ "$status" -eq 0 ] && [ -n "$secret" ]; then
            break
        fi
        if [ "$status" -eq 0 ]; then
            # Succeeded without a password: a credential may exist that we can't use, so retry.
            echo "credential reset returned no password; retrying as temporarily unavailable" >"$err"
        fi
        cat "$err" >&2
        if [ "$attempt" -ge "$max_attempts" ] || ! is_transient "$err"; then
            rm -f "$err"
            echo "Failed to create the ephemeral client secret" >&2
            return 1
        fi
        backoff "$attempt"
        attempt=$((attempt + 1))
    done
    rm -f "$err"

    echo "::add-mask::$secret"
    echo "client-secret=$secret" >>"${GITHUB_OUTPUT:-/dev/null}"
    echo "✅ Created ephemeral client secret $name (expires in 1 hour)"
}

delete() {
    local deleted status
    deleted="$(delete_all "$1" "$2")"
    status=$?
    [ -n "$deleted" ] && printf '%s\n' "$deleted"
    [ "$status" -eq 0 ] || return "$status"
    if [ -z "$deleted" ]; then
        echo "No credential named $2 to delete"
    else
        echo "✅ Ephemeral client secret deleted successfully"
    fi
}

if [ "$#" -ne 3 ] || { [ "$1" != create ] && [ "$1" != delete ]; }; then
    echo "Usage: $0 create|delete <app-id> <display-name>" >&2
    exit 2
fi

"$1" "$2" "$3"
