#!/bin/bash
set -e

echo "=== Setting up Menlo Ralph devcontainer ==="

WORKSPACE_DIR="/workspaces/menlo"
cd "$WORKSPACE_DIR"

# Ensure vscode user owns the workspace
echo "Fixing workspace permissions..."
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

if ! grep -q '$(which npx)' "$HOME/.bashrc" 2>/dev/null; then
    echo 'export PATH="$(npx -y mise@latest which shims):$PATH"' >> "$HOME/.bashrc"
fi

echo "Node: $(node --version)"

# ============================================
# PNPM SETUP
# ============================================
echo "Setting up pnpm..."
corepack enable
corepack prepare pnpm@latest --activate

# Add corepack shims to PATH immediately
export PATH="$HOME/.local/share/node/corepack/shims:$PATH"

# Verify pnpm is available
if command -v pnpm &> /dev/null; then
    echo "pnpm: $(pnpm --version)"
else
    echo "Warning: pnpm not found in PATH after corepack setup"
    echo "PATH: $PATH"
fi

# ============================================
# DOCKER SETUP FOR ASPIRE
# ============================================
echo "Configuring Docker for Aspire..."
# Wait for Docker to be ready
while ! docker info > /dev/null 2>&1; do
    echo "Waiting for Docker to be ready..."
    sleep 2
done

# Add vscode user to docker group if not already
if ! groups vscode | grep -q docker; then
    sudo usermod -aG docker vscode
fi

echo "Docker is ready for Aspire container orchestration"

# ============================================
# ASPIRE CLI
# ============================================
echo "Installing Aspire CLI..."
curl -sSL https://aspire.dev/install.sh | bash
export PATH="$HOME/.aspire/bin:$PATH"

if ! grep -q '.aspire/bin' "$HOME/.bashrc" 2>/dev/null; then
    echo 'export PATH="$HOME/.aspire/bin:$PATH"' >> "$HOME/.bashrc"
fi

echo "Trust dev certificates for Aspire..."
dotnet dev-certs https --trust || echo "Warning: Could not trust dev certificates"

# ============================================
# CLAUDE CODE CLI
# ============================================
echo "Installing Claude Code CLI..."
npm install -g @anthropic-ai/claude-code

# ============================================
# CLEANUP BUILD ARTIFACTS
# ============================================
echo "Cleaning up previous build artifacts..."
# Remove any obj/bin directories from Windows filesystem to prevent conflicts
sudo rm -rf "$WORKSPACE_DIR"/src/*/*/obj "$WORKSPACE_DIR"/src/*/*/bin 2>/dev/null || true
# Clean up /tmp build directory
rm -rf /tmp/menlo-build 2>/dev/null || true

# ============================================
# PROJECT DEPENDENCIES
# ============================================
echo "Restoring .NET dependencies..."
dotnet restore Menlo.slnx || echo "Warning: dotnet restore failed"

echo "Installing pnpm dependencies..."
if command -v pnpm &> /dev/null; then
    # Symlink devcontainer .npmrc to vscode user's home for container-specific settings
    if [ -f ".devcontainer/.npmrc" ]; then
        echo "Creating symlink /home/vscode/.npmrc -> $WORKSPACE_DIR/.devcontainer/.npmrc"
        ln -sf "$WORKSPACE_DIR/.devcontainer/.npmrc" /home/vscode/.npmrc
    fi

    # node_modules directories are mounted as Docker volumes (see devcontainer.json)
    # This avoids chmod/symlink issues on Windows-mounted filesystems
    # Just run pnpm install - the volume mounts handle the rest
    pnpm install || echo "Warning: pnpm install failed"
else
    echo "Skipping pnpm install - pnpm not available (will be available after container restart)"
fi

echo "Building .NET solution..."
dotnet build --no-incremental || echo "Warning: dotnet build failed"

# ============================================
# PLAYWRIGHT SETUP
# ============================================
echo "Setting up Playwright browsers..."
if command -v pnpm &> /dev/null; then
    # Install Playwright browsers and system dependencies
    npx -y playwright@latest install --with-deps chromium || echo "Warning: Playwright browser installation failed"
    echo "Playwright chromium browser installed"
else
    echo "Skipping Playwright setup - pnpm not available (will be available after container restart)"
fi

# ============================================
# VERIFICATION
# ============================================
echo ""
echo "=== Tools ==="
echo "dotnet: $(dotnet --version 2>/dev/null || echo 'not found')"
echo "aspire: $(aspire --version 2>/dev/null || echo 'not found')"
echo "docker: $(docker --version 2>/dev/null || echo 'not found')"
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
echo ""
echo "=== Verify Setup ==="
echo "dotnet build     - Build the solution"
echo "pnpm install     - Install frontend dependencies"
echo "aspire run       - Start Aspire AppHost"
