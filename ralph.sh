#!/bin/bash
set -e

echo "Ralph Wiggum Loop - Continuous Build"
echo "====================================="
echo "Starting GitHub Copilot build loop..."
echo "Press Ctrl+C to stop"

# read issue number from --issue argument
issueNumber=""
while [[ "$1" == --* ]]; do
    case "$1" in
        --issue)
            shift
            issueNumber="$1"
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
    shift
done

while :; do
    echo ""
    echo "=== New Loop Iteration ==="
    date
    prompt=$(cat PROMPT_BUILD.md)
    # replace $issueNumber with the actual issue number in the prompt
    prompt=${prompt//\$issueNumber/$issueNumber}
    copilot --yolo -p "$prompt" --autopilot
    echo "Loop iteration complete. Sleeping 5s..."
    sleep 5
done
