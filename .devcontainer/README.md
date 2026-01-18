# Dev Container Configuration for Windows + WSL2 + Podman/Docker

## Overview

This dev container is configured to work seamlessly with:
- **Host OS**: Windows
- **Container Runtime**: Podman or Docker (via WSL2)
- **Workspace Location**: Windows filesystem mounted via WSL2
- **Aspire Support**: Full container orchestration with Docker-in-Docker

## Aspire Integration

The dev container is configured for .NET Aspire with:
- **Docker-in-Docker**: Enables Aspire to orchestrate PostgreSQL, Ollama, and other containers
- **Resource Allocation**: 8 CPUs, 32GB RAM, 64GB storage for container workloads
- **Persistent Volumes**: 
  - `menlo-postgres` - PostgreSQL database data
  - `menlo-ollama` - AI model storage (phi4-mini, qwen2.5vl)
- **Port Forwarding**: Automatic forwarding for Aspire Dashboard, Angular UI, and Storybook
- **Certificate Trust**: Development certificates are automatically trusted

## The Filesystem Challenge

When running containers on Windows with WSL2, the workspace directory is typically on an NTFS volume that:
- Doesn't support Unix hardlinks/reflinks properly
- Has emulated Unix permissions that don't work well with Node.js package managers
- Can cause EPERM (operation not permitted) errors when tools try to create hardlinks

## Our Solution

### 1. Container Volumes for Package Storage

We use dedicated container volumes for directories that need real Unix filesystem capabilities:

```json
"mounts": [
  "source=menlo-pnpm-store,target=/home/vscode/.pnpm-store,type=volume",
  "source=menlo-node-modules,target=/workspaces/menlo/node_modules,type=volume"
]
```

**Benefits:**
- ✅ `node_modules` lives in a native Linux filesystem with proper permissions
- ✅ pnpm store uses a persistent volume for fast rebuilds
- ✅ No permission conflicts between host and container
- ✅ Faster package installation (no cross-filesystem operations)

### 2. pnpm Configuration

**Root `.npmrc` (Workspace):**
The root `.npmrc` file contains settings that work across all environments:

```properties
# Use copy instead of hardlink for Windows filesystem compatibility
package-import-method=copy

# Auto-install peer dependencies to reduce warnings
auto-install-peers=true
```

**Devcontainer `.npmrc` (`.devcontainer/.npmrc`):**
The devcontainer-specific configuration is applied via `pnpm config` in the post-create script:

```bash
# Store points to the container volume
pnpm config set store-dir /home/vscode/.pnpm-store --location project
```

This approach:
- ✅ Keeps the root `.npmrc` compatible with CI/CD environments
- ✅ Allows devcontainer to use optimized volume storage
- ✅ Works on Windows host without breaking GitHub Actions
- ✅ Automatically configured during container creation

### 3. How It Works

**In the Container:**
- `node_modules` → Container volume (Linux filesystem)
- `.pnpm-store` → Container volume (persisted across rebuilds)
- Workspace code → Bind mount from Windows (read-only for packages)

**On the Host (Windows):**
- You won't see `node_modules` in your workspace (it's in the container)
- If you need to run `pnpm` on the host, packages will be installed separately
- This is intentional - keeps host and container environments independent

## Common Operations

### Running Aspire

```bash
# Start the Aspire AppHost with all services
aspire run

# The dashboard will be available at:
# https://menlo.dev.localhost:17188 (HTTPS)
# http://menlo.dev.localhost:15000 (HTTP)
```

**Services Orchestrated by Aspire:**
- PostgreSQL database (persistent storage)
- Ollama AI service with phi4-mini and qwen2.5vl models
- PgAdmin for database management
- OpenWebUI for AI model interaction
- Menlo API (.NET)
- Angular frontend

### Installing Packages in the Container

```bash
pnpm install
```

This installs to the container volume and works without permission issues.

### Installing Packages on the Host

If you need to run commands on Windows (outside the container):

```powershell
# From Windows terminal
pnpm install
```

This creates a separate `node_modules` on your host, which is fine for editor IntelliSense.

### Adding New Packages

**Inside the container:**
```bash
pnpm add <package-name>
```

The `pnpm-lock.yaml` will be updated on the Windows filesystem (bind mount) and can be committed to git.

### Cleaning Up

**Remove node_modules volume (inside container):**
```bash
sudo rm -rf /workspaces/menlo/node_modules/*
pnpm install
```

**Or recreate the volume entirely:**
```bash
# Exit the container first, then from your host terminal:
podman volume rm menlo-node-modules
# Rebuild the container
```

## Troubleshooting

### EPERM Errors

If you see `EPERM: operation not permitted` errors:
1. Check that volumes are properly mounted (`mount | grep menlo`)
2. Verify `.npmrc` has `package-import-method=copy`
3. Try removing and recreating the volumes

### "node_modules not found" on Host

This is expected! The `node_modules` directory only exists inside the container volume. If you need IntelliSense on the host:
- Run `pnpm install` on your Windows host separately
- The host and container will have independent `node_modules`
- Git will ignore both

### Slow Package Installation

The first install will be slow as packages download. Subsequent installs should be faster because:
- The pnpm store volume is persistent
- Packages are cached across container rebuilds

### Volume Inspection

To see what's in the volumes:
```bash
# From inside the container
ls -la /workspaces/menlo/node_modules
ls -la /home/vscode/.pnpm-store

# From the host (with podman)
podman volume ls
podman volume inspect menlo-node-modules
```

## Why Not Use Symlinks?

Previous approaches tried using symlinks (e.g., `node_modules -> /tmp/menlo-node-modules`), but this fails because:
- Symlinks don't solve the underlying filesystem permission issues
- The target directory can be deleted/recreated, breaking the link
- Container volumes are the "Docker/Podman way" and more reliable

## References

- [VS Code Dev Containers: Advanced Containers](https://code.visualstudio.com/remote/advancedcontainers/improve-performance)
- [pnpm on Windows: Best Practices](https://pnpm.io/faq#pnpm-does-not-work-with-your-project-here)
- [Podman Volume Management](https://docs.podman.io/en/latest/markdown/podman-volume.1.html)
