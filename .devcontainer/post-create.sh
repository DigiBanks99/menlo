#!/bin/bash
set -e

echo "=== Setting up Menlo devcontainer ==="

WORKSPACE_DIR="/workspaces/menlo"
cd "$WORKSPACE_DIR"

# Ensure vscode user owns the workspace
echo "Fixing workspace permissions..."
sudo chown -R vscode:vscode /workspaces/menlo

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
curl -fsSL https://claude.ai/install.sh | bash

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

echo "Building .NET solution..."
dotnet build --no-incremental || echo "Warning: dotnet build failed"

# ============================================
# VERIFICATION
# ============================================
echo ""
echo "=== Tools ==="
echo "dotnet: $(dotnet --version 2>/dev/null || echo 'not found')"
echo "aspire: $(aspire --version 2>/dev/null || echo 'not found')"
echo "docker: $(docker --version 2>/dev/null || echo 'not found')"
echo "gh: $(gh --version 2>/dev/null | head -1 || echo 'not found')"
echo "claude: $(claude --version 2>/dev/null || echo 'not found')"
echo "git: $(git --version 2>/dev/null || echo 'not found')"

echo ""
echo "=== Ready! ==="
echo "claude           - Start Claude Code"
echo ""
echo "=== Verify Setup ==="
echo "dotnet build     - Build the solution"
echo "aspire run       - Start Aspire AppHost"
