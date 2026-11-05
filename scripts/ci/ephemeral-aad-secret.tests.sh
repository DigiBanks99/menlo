#!/usr/bin/env bash
# Black-box tests for scripts/ci/ephemeral-aad-secret.sh against a fake `az` on PATH.
# The fake keeps credentials in a temp dir and plays a per-operation queue of failures.
# Run: bash scripts/ci/ephemeral-aad-secret.tests.sh
set -u

script_dir="$(cd "$(dirname "$0")" && pwd)"
subject="$script_dir/ephemeral-aad-secret.sh"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

mkdir -p "$work/bin"
cat >"$work/bin/az" <<'FAKE'
#!/usr/bin/env bash
# Fake `az ad app credential <reset|list|delete>`. State lives in $FAKE_AZ_STATE:
#   creds/<keyId>  holds the credential's display name
#   queue_<op>     one action per call: throttle, denied, done-then-throttle (reset/delete), empty-password (reset);
#                  an empty queue means succeed
#   calls          one line per call, the operation name
state="$FAKE_AZ_STATE"
op="$4"
shift 4
name="" key_id="" query=""
while [ "$#" -gt 0 ]; do
    case "$1" in
        --display-name) name="$2"; shift 2 ;;
        --key-id) key_id="$2"; shift 2 ;;
        --query) query="$2"; shift 2 ;;
        --append) shift ;;
        *) shift 2 ;;
    esac
done
echo "$op" >>"$state/calls"

action=""
queue="$state/queue_$op"
if [ -s "$queue" ]; then
    action="$(head -n 1 "$queue")"
    sed -i 1d "$queue"
fi

throttle() {
    echo "ERROR: Error due to concurrent requests being made to the tenant. Please wait briefly and retry." >&2
    exit 1
}
case "$action" in
    throttle) throttle ;;
    denied) echo "ERROR: Insufficient privileges to complete the operation." >&2; exit 1 ;;
esac

case "$op" in
    reset)
        id="key-$(date +%s%N)-$RANDOM"
        echo "$name" >"$state/creds/$id"
        [ "$action" = done-then-throttle ] && throttle
        [ "$action" = empty-password ] && exit 0
        echo "s3cr3t-$id"
        ;;
    list)
        wanted="$(printf '%s' "$query" | sed "s/.*displayName=='\([^']*\)'.*/\1/")"
        for f in "$state"/creds/*; do
            [ -e "$f" ] || continue
            [ "$(cat "$f")" = "$wanted" ] && basename "$f"
        done
        ;;
    delete)
        [ -e "$state/creds/$key_id" ] || { echo "ERROR: credential $key_id not found" >&2; exit 1; }
        rm "$state/creds/$key_id"
        [ "$action" = done-then-throttle ] && throttle
        ;;
esac
exit 0
FAKE
chmod +x "$work/bin/az"

NAME="CI-Ephemeral-42"
failures=0
passes=0
current=""
state=""
status=0

# setup <case-name>: fresh fake state
setup() {
    current="$1"
    state="$work/case-$((passes + failures))-$RANDOM"
    mkdir -p "$state/creds"
    : >"$state/calls"
    : >"$state/output"
}

# queue <op> <action...>
queue() {
    local op="$1"
    shift
    printf '%s\n' "$@" >>"$state/queue_$op"
}

# seed <display-name>: an existing credential
seed() {
    echo "$1" >"$state/creds/key-seed-$RANDOM"
}

# run <create|delete>
run() {
    FAKE_AZ_STATE="$state" AZ_RETRY_DELAYS="0 0" GITHUB_OUTPUT="$state/output" PATH="$work/bin:$PATH" \
        bash "$subject" "$1" app-id "$NAME" >"$state/stdout" 2>"$state/stderr"
    status=$?
}

calls() { grep -c "^$1\$" "$state/calls"; }
creds_named() { grep -lx "$1" "$state"/creds/* 2>/dev/null | wc -l | tr -d ' '; }

check() {
    local description="$1"
    shift
    if "$@"; then
        passes=$((passes + 1))
    else
        failures=$((failures + 1))
        echo "FAIL [$current] $description"
        sed 's/^/    stderr: /' "$state/stderr"
    fi
}

eq() { [ "$1" = "$2" ]; }
outputs_set() {
    grep -q '^client-secret=s3cr3t-' "$state/output"
}
# The secret in the output belongs to the credential that is left.
secret_is_survivor() {
    local survivor
    survivor="$(basename "$(grep -lx "$NAME" "$state"/creds/*)")"
    grep -qx "client-secret=s3cr3t-$survivor" "$state/output"
}
secret_masked_only() {
    local secret
    secret="$(sed -n 's/^client-secret=//p' "$state/output")"
    [ -n "$secret" ] && grep -qx "::add-mask::$secret" "$state/stdout" &&
        [ "$(grep -c -- "$secret" "$state/stdout")" -eq 1 ] && ! grep -q -- "$secret" "$state/stderr"
}

setup "create succeeds first time"
run create
check "exits 0" eq "$status" 0
check "sets outputs" outputs_set
check "leaves one credential" eq "$(creds_named "$NAME")" 1
check "masks the secret and prints it nowhere else" secret_masked_only

setup "reset throttled once"
queue reset throttle
run create
check "exits 0" eq "$status" 0
check "resets twice" eq "$(calls reset)" 2
check "sets outputs" outputs_set
check "leaves one credential" eq "$(creds_named "$NAME")" 1

setup "reset created the credential but the response was throttled"
queue reset done-then-throttle
run create
check "exits 0" eq "$status" 0
check "resets twice" eq "$(calls reset)" 2
check "deletes the orphan" eq "$(calls delete)" 1
check "leaves one credential" eq "$(creds_named "$NAME")" 1
check "outputs the surviving credential's secret" secret_is_survivor

setup "orphan lookup throttled once"
queue reset done-then-throttle
queue list throttle
run create
check "exits 0" eq "$status" 0
check "lists twice" eq "$(calls list)" 2
check "leaves one credential" eq "$(creds_named "$NAME")" 1
check "outputs the surviving credential's secret" secret_is_survivor

setup "reset succeeded without a password"
queue reset empty-password
run create
check "exits 0" eq "$status" 0
check "resets twice" eq "$(calls reset)" 2
check "leaves one credential" eq "$(creds_named "$NAME")" 1
check "outputs the surviving credential's secret" secret_is_survivor

setup "leftover from an earlier run attempt is removed by cleanup"
seed "$NAME"
seed "Someone-Else"
run create
check "create exits 0" eq "$status" 0
run delete
check "delete exits 0" eq "$status" 0
check "leaves no credential with the name" eq "$(creds_named "$NAME")" 0
check "keeps other credentials" eq "$(creds_named Someone-Else)" 1

setup "reset throttled every time"
queue reset throttle throttle throttle throttle
run create
check "fails" eq "$status" 1
check "tries 3 times" eq "$(calls reset)" 3
check "leaves no credential" eq "$(creds_named "$NAME")" 0
check "sets no outputs" eq "$(wc -c <"$state/output" | tr -d ' ')" 0

setup "reset denied"
queue reset denied
run create
check "fails" eq "$status" 1
check "tries once" eq "$(calls reset)" 1
check "reports the error" grep -q "Insufficient privileges" "$state/stderr"

setup "orphan lookup denied"
queue reset throttle
queue list denied
run create
check "fails" eq "$status" 1
check "lists once" eq "$(calls list)" 1
check "does not reset again" eq "$(calls reset)" 1

setup "delete removes every credential with the name"
seed "$NAME"
seed "$NAME"
seed "Someone-Else"
run delete
check "exits 0" eq "$status" 0
check "removes them" eq "$(creds_named "$NAME")" 0
check "keeps other credentials" eq "$(creds_named Someone-Else)" 1

setup "delete with nothing to delete"
run delete
check "exits 0" eq "$status" 0
check "says so" grep -q "No credential named" "$state/stdout"

setup "delete throttled once"
seed "$NAME"
queue delete throttle
run delete
check "exits 0" eq "$status" 0
check "removes it" eq "$(creds_named "$NAME")" 0

setup "delete went through but the response was throttled"
seed "$NAME"
queue delete done-then-throttle
run delete
check "exits 0" eq "$status" 0
check "removes it" eq "$(creds_named "$NAME")" 0

setup "delete denied"
seed "$NAME"
queue delete denied
run delete
check "fails" eq "$status" 1
check "tries once" eq "$(calls delete)" 1

echo "$passes passed, $failures failed"
[ "$failures" -eq 0 ]
