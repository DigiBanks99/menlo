#!/bin/bash
set -e

echo "=== Setting up Menlo Ralph devcontainer ==="

WORKSPACE_DIR="/workspaces/menlo"
cd "$WORKSPACE_DIR"

sudo chown -R vscode:vscode /workspaces/menlo

# ============================================
# MISE SETUP
# ============================================
echo "Setting up mise..."
export PATH="$HOME/.local/bin:$HOME/.local/share/mise/bin:$PATH"

if ! command -v mise &> /dev/null; then
    echo "Installing mise..."
    curl https://mise.run | sh
fi

mise trust || true
mise install
eval "$(mise activate bash --shims)"

if ! grep -q 'mise activate' "$HOME/.bashrc" 2>/dev/null; then
    echo 'eval "$(mise activate bash)"' >> "$HOME/.bashrc"
fi

echo "Node: $(node --version)"

# ============================================
# PNPM SETUP
# ============================================
echo "Setting up pnpm..."
corepack enable
corepack prepare pnpm@latest --activate
echo "pnpm: $(pnpm --version)"

# Configure pnpm for Windows-mounted volumes (WSL2 workaround)
# The 9p filesystem used by WSL2 has permission issues with pnpm's default behavior
# Solution: Use Linux-native locations for both store and node_modules
if [ ! -L "$WORKSPACE_DIR/node_modules" ]; then
    echo "Setting up node_modules symlink workaround for WSL2..."
    rm -rf "$WORKSPACE_DIR/node_modules"
    mkdir -p /tmp/menlo-node-modules
    ln -s /tmp/menlo-node-modules "$WORKSPACE_DIR/node_modules"
fi

# Use Linux-native pnpm store and copy method
pnpm config set package-import-method copy
pnpm config set store-dir /tmp/pnpm-store

# ============================================
# ASPIRE CLI
# ============================================
echo "Installing Aspire CLI..."
curl -sSL https://aspire.dev/install.sh | bash
export PATH="$HOME/.aspire/bin:$PATH"

if ! grep -q '.aspire/bin' "$HOME/.bashrc" 2>/dev/null; then
    echo 'export PATH="$HOME/.aspire/bin:$PATH"' >> "$HOME/.bashrc"
fi

# ============================================
# CLAUDE CODE CLI
# ============================================
echo "Installing Claude Code CLI..."
npm install -g @anthropic-ai/claude-code

# ============================================
# PROJECT DEPENDENCIES
# ============================================
echo "Restoring .NET dependencies..."
dotnet restore Menlo.slnx || echo "Warning: dotnet restore failed"

echo "Installing npm dependencies..."
pnpm install || echo "Warning: root pnpm install failed"

echo "Installing frontend dependencies..."
pnpm --dir src/ui/web install || echo "Warning: frontend pnpm install failed"

# ============================================
# VERIFICATION
# ============================================
echo ""
echo "=== Tools ==="
echo "dotnet: $(dotnet --version 2>/dev/null || echo 'not found')"
echo "aspire: $(aspire --version 2>/dev/null || echo 'not found')"
echo "node: $(node --version 2>/dev/null || echo 'not found')"
echo "pnpm: $(pnpm --version 2>/dev/null || echo 'not found')"
echo "gh: $(gh --version 2>/dev/null | head -1 || echo 'not found')"
echo "claude: $(claude --version 2>/dev/null || echo 'not found')"
echo "git: $(git --version 2>/dev/null || echo 'not found')"
echo "mise: $(mise --version 2>/dev/null || echo 'not found')"

echo ""
echo "=== Ready! ==="
echo "./ralph.sh plan  - Generate fix_plan.md"
echo "./ralph.sh build - Start build loop"
