#!/usr/bin/env bash
# Smoke-checks a deployed Menlo instance (API with the bundled SPA).
# Retries until every check passes or the time runs out, then exits 1.
#
# Usage: deployment/smoke-check.sh <base-url> [timeout-seconds]
# Example: deployment/smoke-check.sh https://menlo.wilcob.co.za 120

set -uo pipefail

BASE_URL="${1:?usage: smoke-check.sh <base-url> [timeout-seconds]}"
BASE_URL="${BASE_URL%/}"
TIMEOUT_SECONDS="${2:-120}"
RETRY_INTERVAL_SECONDS=10
USER_AGENT="Menlo-Smoke-Check"

FAILURES=()

# check <path> <expected status> [expected content type] [required header]
check() {
  local path="$1" expected_status="$2" expected_type="${3:-}" required_header="${4:-}"
  local headers status content_type

  headers=$(curl -sS -o /dev/null -D - --max-time 10 -A "$USER_AGENT" "$BASE_URL$path" 2>&1 | tr -d '\r')
  status=$(awk '/^HTTP\//{code=$2} END{print code}' <<<"$headers")
  content_type=$(awk -F': *' 'tolower($1)=="content-type"{print $2}' <<<"$headers" | tail -n1)

  if [[ "$status" != "$expected_status" ]]; then
    FAILURES+=("GET $path: expected $expected_status, got ${status:-no response}")
  elif [[ -n "$expected_type" && "$content_type" != "$expected_type"* ]]; then
    FAILURES+=("GET $path: expected $expected_type, got ${content_type:-no content type}")
  elif [[ -n "$required_header" ]] && ! grep -qi "^$required_header:" <<<"$headers"; then
    FAILURES+=("GET $path: missing $required_header header")
  fi
}

run_checks() {
  FAILURES=()
  check /health 200
  check /api/budgets 401
  check / 200 text/html Content-Security-Policy
  check /some/deep/link 200 text/html
}

deadline=$((SECONDS + TIMEOUT_SECONDS))
attempt=1

while true; do
  run_checks
  if [[ ${#FAILURES[@]} -eq 0 ]]; then
    echo "✅ Smoke check passed for $BASE_URL (attempt $attempt)"
    exit 0
  fi

  if (( SECONDS + RETRY_INTERVAL_SECONDS > deadline )); then
    break
  fi

  echo "⏳ Attempt $attempt: ${#FAILURES[@]} check(s) failing, retrying in ${RETRY_INTERVAL_SECONDS}s..."
  sleep "$RETRY_INTERVAL_SECONDS"
  attempt=$((attempt + 1))
done

echo "::error::Smoke check failed for $BASE_URL after ${TIMEOUT_SECONDS}s ($attempt attempts)"
for failure in "${FAILURES[@]}"; do
  echo "  ❌ $failure"
done
exit 1
