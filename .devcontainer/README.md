# Dev container

A dev container for working on Menlo on Windows with WSL2 and Podman or Docker. It has the .NET SDK, Docker-in-Docker for Aspire, the GitHub CLI, the Aspire CLI and Claude Code. There is no Node toolchain: the UI is server-rendered by the API (see [ADR-007](../docs/decisions/adr-007-server-rendered-ui.md)).

## What it sets up

- **Docker-in-Docker**, so Aspire can run PostgreSQL, llama.cpp and pgAdmin.
- **Resources**: 8 CPUs, 32 GB RAM and 64 GB storage for those containers.
- **A persistent volume**, `menlo-postgres`, for the database.
- **Port forwarding** for the Aspire dashboard (15000 and 17188).
- **Trusted development certificates.**

`post-create.sh` installs the Aspire CLI and Claude Code, restores and builds the solution.

## Build output

The workspace is on the Windows filesystem, which doesn't support the Unix file modes a .NET build sets. The container sets `MENLO_DEV_CONTAINER=true`, and `Directory.Build.props` then sends `bin` and `obj` to `/tmp/menlo-build`.

## Running the app

```bash
aspire run
```

The dashboard is at `https://menlo.dev.localhost:17188`. Aspire starts:

- PostgreSQL, with pgAdmin
- the llama.cpp server, serving `qwen3.5-4b` for chat and vision
- the Menlo API, which also serves the UI

## Troubleshooting

- **Build errors about file permissions**: check that `MENLO_DEV_CONTAINER` is `true` inside the container (`echo $MENLO_DEV_CONTAINER`).
- **Aspire can't start containers**: check that Docker is up with `docker info`. `post-create.sh` waits for it, so a failure here usually means the Docker-in-Docker feature didn't start; rebuild the container.
- **A stale database**: remove the volume with `podman volume rm menlo-postgres` (or `docker volume rm`), then rebuild.
