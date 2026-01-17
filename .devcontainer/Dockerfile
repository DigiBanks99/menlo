FROM mcr.microsoft.com/devcontainers/base:ubuntu

# Install dependencies for mise and builds
RUN apt-get update && apt-get install -y \
    curl \
    git \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Note: .NET, GitHub CLI, and mise are installed via devcontainer features
# Node.js and pnpm are managed by mise (configured in repo's mise.toml)
