FROM mcr.microsoft.com/devcontainers/base:ubuntu

# Install dependencies for mise and builds
RUN apt-get update && apt-get install -y \
    curl \
    git \
    build-essential \
    && rm -rf /var/lib/apt/lists/*
