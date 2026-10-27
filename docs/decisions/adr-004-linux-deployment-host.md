# ADR-004: Linux Deployment Host

## Status

**Accepted** - 2026-10-27

Supersedes the Windows 10 Home + Podman/WSL2 host assumption in [ADR-001](adr-001-hosting-strategy.md) and [ADR-003](adr-003-cd-authentication-strategy.md).

## Context

ADR-001 placed the backend, PostgreSQL and Ollama on a "home server", and the deployment tooling assumed that server was a Windows 10 Home machine running Podman inside WSL2:

- `cd-backend.yml` ran its deploy steps with `shell: pwsh` and Windows-only commands (`$env:USERPROFILE`, `Restart-Service`, `Invoke-WebRequest`)
- `deployment/` shipped PowerShell scripts to set up WSL2, Podman Desktop and the `cloudflared` Windows service

The `menlo-deploy` self-hosted runner has since been replaced by a Linux host. PowerShell is not installed there, so every backend deployment has failed at the first deploy step with `pwsh: command not found` ([#516](https://github.com/DigiBanks99/menlo/issues/516)).

### Requirements

- **Reliability**: Deployments must run on the host that actually exists
- **Simplicity**: Avoid extra runtimes on the host just to run deploy scripts
- **Fit**: The workload is all Linux containers (API, PostgreSQL, Ollama)

## Options Considered

### 1. Install PowerShell on the Linux host

- **Pros**: No workflow changes
- **Cons**: Extra runtime to install and patch only to run deploy scripts; the scripts still use Windows-only cmdlets (`Restart-Service`, `$env:USERPROFILE`) and would need rewriting anyway
- **Assessment**: Treats the symptom, not the cause

### 2. Return to a Windows host

- **Pros**: Matches the existing scripts
- **Cons**: Linux containers on Windows need WSL2 and a Podman machine VM, which adds overhead and more ways to fail; Windows 10 is out of support
- **Assessment**: Not worth reversing a host change that already happened

### 3. Run on Linux and write the deploy steps in bash (Selected)

- **Pros**: Containers run natively without a VM; bash, `curl`, `base64` and `systemd` are on every distribution; `cloudflared` runs as a standard systemd service
- **Cons**: Windows-specific docs and scripts have to be rewritten or removed
- **Assessment**: Matches the real host and keeps the deploy toolchain to the basics

## Decision

Production runs on a **Linux host**. The `menlo-deploy` self-hosted runner lives on that host and deploys to it directly.

- Deploy steps in `cd-backend.yml` that run on `menlo-deploy` use `shell: bash`
- Containers run under Podman (preferred) or Docker, through `<runtime> compose`
- `cloudflared` runs as a systemd service
- Windows-only deployment scripts are removed from `deployment/`

The rest of ADR-001 stays the same: Cloudflare Pages frontend, Cloudflare Tunnel connectivity, and local PostgreSQL and Ollama. The certificate-based Entra authentication from ADR-003 also stays the same; the certificate is passed to the container as Base64, which does not depend on the host OS.

Windows remains a supported **development** platform. Developer tooling such as `scripts/*.ps1` and `.github/hooks/*.ps1`, and CI jobs on GitHub-hosted runners, are not affected.

## Consequences

### Positive

- Backend deployments work again
- No WSL2 or Podman machine VM between the containers and the host
- One shell (bash) for every job in `cd-backend.yml`

### Negative

- Anyone who set up the old Windows host has to follow the Linux setup guide instead
- `docs/requirements/**` may still mention Windows; those specs are read-only and are updated separately

### Host prerequisites

- Linux distribution with systemd
- Podman with a compose provider (`podman compose`), or Docker with the compose plugin
- `curl` and GNU coreutils (`base64 -w0`)
- GitHub Actions runner registered with the `menlo-deploy` label
- `cloudflared` installed as a systemd service, with passwordless `sudo` for `systemctl restart cloudflared` for the runner user if `CLOUDFLARE_TUNNEL_ENABLED=true`

## Related Documents

- [ADR-001: Hosting Strategy](adr-001-hosting-strategy.md)
- [ADR-003: CD Pipeline Authentication Strategy](adr-003-cd-authentication-strategy.md)
- [Deployment guide](../../deployment/README.md)
