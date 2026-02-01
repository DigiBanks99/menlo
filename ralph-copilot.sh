#!/bin/bash
set -e

# Copilot CLI Loop Script
# Equivalent to ralph.sh but uses GitHub Copilot CLI instead of Claude CLI
# Supports test, plan, and build modes for orchestrating AI-assisted development

MODE=${1:-test}

echo "Copilot CLI Loop - Mode: $MODE"
echo "================================"

# Workspace directory for file access
WORKSPACE_DIR="/workspaces/menlo"

# Common options for all Copilot CLI invocations
# --allow-all: Grant all permissions (tools, paths, URLs)
# --add-dir: Ensure access to workspace files
# --stream on: Enable streaming output
COMMON_OPTS="--allow-all --add-dir $WORKSPACE_DIR --stream on --no-auto-update --log-level all"

if [ "$MODE" = "test" ]; then
    # Test mode: Quick connectivity check with minimal model
    echo "Running test mode (single execution with gpt-5-mini)..."
    echo "Testing Copilot CLI connectivity..."
    copilot -p "Hello, please confirm you can access the workspace at $WORKSPACE_DIR. List the top-level directories." \
        $COMMON_OPTS \
        --model gpt-5-mini
    echo ""
    echo "Test complete. If you see a response above, Copilot CLI is working."

elif [ "$MODE" = "plan" ]; then
    # Plan mode: Single execution for strategic planning
    # Uses most capable model (Opus equivalent)
    echo "Running planning loop (single execution)..."
    echo "Using model: claude-opus-4.5"
    echo ""
    copilot -p "$(cat PROMPT_PLAN_COPILOT.md)" \
        $COMMON_OPTS \
        --model claude-opus-4.5 \
        --share ./copilot_plan_session.md
    echo ""
    echo "Planning complete. Check docs/plans/fix_plan.md and copilot_plan_session.md"

elif [ "$MODE" = "build" ]; then
    # Build mode: Continuous implementation loop
    # Uses balanced model (Sonnet equivalent)
    echo "Starting build loop (continuous)..."
    echo "Using model: claude-sonnet-4"
    echo "Press Ctrl+C to stop"
    echo ""

    ITERATION=0
    while :; do
        ITERATION=$((ITERATION + 1))
        echo ""
        echo "=== Loop Iteration #$ITERATION ==="
        date

        # Create timestamped session file for this iteration
        SESSION_FILE="./copilot_build_session_$(date +%Y%m%d_%H%M%S).md"

        copilot -p "$(cat PROMPT_BUILD_COPILOT.md)" \
            $COMMON_OPTS \
            --model claude-sonnet-4 \
            --share "$SESSION_FILE"

        echo "Loop iteration complete. Session saved to: $SESSION_FILE"
        echo "Sleeping 5s..."
        sleep 5
    done
    
else
    # Help text
    echo "Usage: ralph-copilot.sh [test|plan|build]"
    echo ""
    echo "Modes:"
    echo "  test  - Test Copilot CLI connectivity with minimal prompt"
    echo "          Uses: gpt-5-mini (fast, low cost)"
    echo ""
    echo "  plan  - Generate/regenerate fix_plan.md (single run)"
    echo "          Uses: claude-opus-4.5 (most capable, for strategic planning)"
    echo ""
    echo "  build - Continuous implementation loop"
    echo "          Uses: claude-sonnet-4 (balanced, for implementation)"
    echo ""
    echo "Available Models (edit script to change):"
    echo "  - gpt-5-mini          (fastest, lowest cost - for testing)"
    echo "  - claude-sonnet-4     (balanced - default for build)"
    echo "  - claude-opus-4.5     (most capable - default for plan)"
    echo "  - gpt-5.1-codex-max   (alternative for planning)"
    echo ""
    echo "Examples:"
    echo "  ./ralph-copilot.sh test   # Test CLI connectivity"
    echo "  ./ralph-copilot.sh plan   # Run strategic planning"
    echo "  ./ralph-copilot.sh build  # Start continuous build loop"
    echo ""
    exit 1
fi
