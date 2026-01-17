#!/bin/bash
set -e

MODE=${1:-build}

echo "Ralph Wiggum Loop - Mode: $MODE"
echo "================================"

if [ "$MODE" = "plan" ]; then
    echo "Running planning loop (single execution)..."
    claude --dangerously-skip-permissions < PROMPT_PLAN.md
    echo "Planning complete. Check docs/plans/fix_plan.md"
elif [ "$MODE" = "build" ]; then
    echo "Starting build loop (continuous)..."
    echo "Press Ctrl+C to stop"
    while :; do
        echo ""
        echo "=== New Loop Iteration ==="
        date
        claude --dangerously-skip-permissions < PROMPT_BUILD.md
        echo "Loop iteration complete. Sleeping 5s..."
        sleep 5
    done
else
    echo "Usage: ralph.sh [plan|build]"
    echo "  plan  - Generate/regenerate fix_plan.md (single run)"
    echo "  build - Continuous implementation loop"
    exit 1
fi
