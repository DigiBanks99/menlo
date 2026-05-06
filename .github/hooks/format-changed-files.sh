#!/usr/bin/env bash

set -u

VERBOSE="${VERBOSE:-0}"
EDIT_TOOL_PATTERN='apply_patch|create_file|replace_string_in_file|multi_replace_string_in_file|edit_notebook_file|write|patch|edit'

stdin_text=""
if [ ! -t 0 ]; then
    stdin_text="$(cat)"
fi

if [ -z "$stdin_text" ]; then
    if [ "$VERBOSE" = "1" ]; then
        echo "Skipping: no hook payload on stdin."
    fi
    exit 0
fi

if ! printf '%s' "$stdin_text" | grep -Eiq "$EDIT_TOOL_PATTERN"; then
    if [ "$VERBOSE" = "1" ]; then
        echo "Skipping: tool event was not an edit/write action."
    fi
    exit 0
fi

files=""
if ! tracked="$(git diff --name-only 2>/dev/null)"; then
    tracked=""
fi
if ! staged="$(git diff --name-only --cached 2>/dev/null)"; then
    staged=""
fi
if ! untracked="$(git ls-files --others --exclude-standard 2>/dev/null)"; then
    untracked=""
fi

files="$(printf '%s\n%s\n%s\n' "$tracked" "$staged" "$untracked" | sed '/^$/d' | sort -u)"

if [ -z "$files" ]; then
    if [ "$VERBOSE" = "1" ]; then
        echo "No changed files found to format."
    fi
    exit 0
fi

net_csv=""
web_args=()

while IFS= read -r file; do
    norm="${file//\\//}"
    if printf '%s' "$norm" | grep -Eiq '\.(cs|csproj|vb)$'; then
        if [ -z "$net_csv" ]; then
            net_csv="$norm"
        else
            net_csv="$net_csv,$norm"
        fi
    elif printf '%s' "$norm" | grep -Eiq '^src/ui/web/.*\.(ts|tsx|js|jsx|json)$'; then
        web_args+=("$norm")
    fi
done << EOF
$files
EOF

has_failure=0

if [ -n "$net_csv" ]; then
    echo "Formatting .NET files..."
    if ! dotnet format --include "$net_csv" --no-restore >/dev/null 2>&1; then
        has_failure=1
        echo "Warning: dotnet format failed or returned non-zero."
    fi
fi

if [ "${#web_args[@]}" -gt 0 ]; then
    echo "Formatting web files..."
    if ! pnpm format -- "${web_args[@]}" >/dev/null 2>&1; then
        has_failure=1
        echo "Warning: pnpm format failed or returned non-zero."
    fi
fi

if [ "$has_failure" -ne 0 ]; then
    printf '%s\n' '{"decision":"block","stopReason":"Formatting failed for one or more changed files.","systemMessage":"Formatting enforcement blocked this action. Fix formatting and retry."}'
    exit 2
fi

exit 0
