# Menlo Deployment - Linux + Docker/Podman + llama.cpp

Deployment guide for the Menlo Home Management application on a **Linux host** using **Docker or Podman containers**, the host's **llama.cpp server**, and **Cloudflare Tunnels**.

## 🎯 Quick Start

This setup is designed for:

- **Linux host with systemd** (see [ADR-004](../docs/decisions/adr-004-linux-deployment-host.md))
- **Docker or Podman** (pick one with the `CONTAINER_RUNTIME` variable)
- **Local llama.cpp** (the `llama-server` already running on the host, shared with the knowledge base)
- **Cloudflare Tunnels** (secure remote access without exposing IPs)
- **Bundled SPA** (the API image serves the Angular 21 frontend, see [ADR-005](../docs/decisions/adr-005-bundled-spa-hosting.md))

## 📁 Files Overview

| File | Purpose |
|------|---------|
| `docker-compose.prod.yml` | Reference production stack (API, PostgreSQL, backups) |
| `README.md` | This guide |

The CD workflow (`.github/workflows/cd.yml`) writes its own `docker-compose.prod.yml` to `~/menlo` on the host on every deploy, with the image tag, Entra settings and certificate filled in.

## 🚀 Initial Setup

### Prerequisites

- Linux distribution with systemd (e.g. Ubuntu 24.04, Oracle Linux 9), on `arm64`. CD only publishes `menlo-api` for `linux/arm64`; the `postgres` image is multi-arch
- `sudo` access for setup
- `curl` and GNU coreutils
- Stable internet connection
- `llama-server` running on the host at `http://127.0.0.1:9931/v1` (see Step 4)
- Nothing else listening on `127.0.0.1:5432` or `127.0.0.1:8080` (the API uses host networking, see Network Security)

### Step 1: Create the Runner User and Install a Container Runtime

```bash
# Dedicated user for the GitHub Actions runner (matches the runner's working dir)
sudo useradd --create-home --shell /bin/bash ghrunner
```

Install **one** of the following, then set the `CONTAINER_RUNTIME` repository variable to match (see Step 3).

#### Option A: Docker

```bash
# Docker Engine + compose plugin (see https://docs.docker.com/engine/install/ for your distribution)
curl -fsSL https://get.docker.com | sudo sh
sudo systemctl enable --now docker

# Let the runner user talk to the Docker daemon
sudo usermod -aG docker ghrunner
```

Docker's daemon restarts `unless-stopped` containers after a reboot, so nothing else is needed.

#### Option B: Podman (rootless)

```bash
sudo apt-get install -y podman podman-compose      # Debian/Ubuntu
# sudo dnf install -y podman podman-compose        # Oracle Linux/RHEL/Fedora

# Let rootless containers keep running and restart after reboots
sudo loginctl enable-linger ghrunner
sudo -iu ghrunner systemctl --user enable --now podman-restart.service
```

Check it works as the runner user (restart the runner service after adding the `docker` group):

```bash
sudo -iu ghrunner docker compose version     # or: podman compose version
```

> **Switching runtimes:** Docker and Podman keep separate volumes. Before switching, back up the database (see Database Backup & Restore below), stop the old stack, and restore into the new one.

### Step 2: Configure Cloudflare Tunnel

Replace `YOUR_DOMAIN` and `YOUR_SUBDOMAIN` with your actual values:

```bash
DOMAIN="example.com"     # Replace with your domain
SUBDOMAIN="api.menlo"    # Or your preferred subdomain

# Install cloudflared (see https://pkg.cloudflare.com for your distribution)

# Login to Cloudflare (prints a URL to open in a browser)
cloudflared tunnel login

# Create tunnel for your API
cloudflared tunnel create menlo-api

# Configure DNS
cloudflared tunnel route dns menlo-api "$SUBDOMAIN.$DOMAIN"

# Create config file (use the credentials file path printed by `tunnel create`)
mkdir -p ~/.cloudflared
cat > ~/.cloudflared/config.yml <<EOF
tunnel: menlo-api
credentials-file: $HOME/.cloudflared/<TUNNEL_ID>.json

ingress:
  - hostname: $SUBDOMAIN.$DOMAIN
    service: http://localhost:8080
  - service: http_status:404
EOF

# Install as a systemd service (copies the config to /etc/cloudflared)
sudo cloudflared service install
sudo systemctl enable --now cloudflared
```

If `CLOUDFLARE_TUNNEL_ENABLED` is `true`, the deploy job restarts the tunnel with `sudo -n systemctl restart cloudflared`. Allow the runner user to do that without a password:

```bash
echo 'ghrunner ALL=(root) NOPASSWD: /usr/bin/systemctl restart cloudflared' | sudo tee /etc/sudoers.d/ghrunner-cloudflared
sudo chmod 440 /etc/sudoers.d/ghrunner-cloudflared
```

> Check the `systemctl` path with `command -v systemctl`; some distributions use `/bin/systemctl`.

### Step 3: Configure GitHub Actions Self-Hosted Runner

> **Current Deployment Strategy**: Menlo uses a self-hosted GitHub Actions runner on the Linux host for deployments,
> providing direct line-of-sight to local services without SSH requirements.

**Set up the self-hosted runner:**

1. **Install GitHub Actions Runner**: Follow [GitHub's guide](https://docs.github.com/en/actions/hosting-your-own-runners/adding-self-hosted-runners) to install the Linux runner as the `ghrunner` user
2. **Register the runner** with the repository and add the `menlo-deploy` label (the deploy job targets `[self-hosted, menlo-deploy]`)
3. **Run it as a service**: `sudo ./svc.sh install ghrunner && sudo ./svc.sh start`
4. **Configure GitHub repository secrets** (see table below)

**Add these secrets to your GitHub repository:**

| Secret Name | Value | Description | Required |
|-------------|-------|-------------|----------|
| `POSTGRES_USER` | `menlo` | Database user | ✅ |
| `POSTGRES_PASSWORD` | `secure_password_here` | Database password | ✅ |
| `MENLO_AI_API_KEY` | the `llama-server` `--api-key` | Key for the host's llama.cpp server; CD fails early without it | ✅ |

**Add these variables to your GitHub repository:**

| Variable Name | Value | Description | Required |
|---------------|-------|-------------|----------|
| `CONTAINER_RUNTIME` | `docker` or `podman` | Container runtime on the host; unset auto-detects, preferring Podman | - |
| `CLOUDFLARE_TUNNEL_ENABLED` | `true` | Enable Cloudflare tunnel restart | - |
| `FRONTEND_URL` | `https://menlo.yourdomain.com` | Production URL (API with the bundled SPA); CD smoke-checks it after every deploy | ✅ |

### Step 4: Prepare the llama.cpp Server

Production does not start a model server. The API calls the `llama-server` the host already runs for the knowledge base, at `http://127.0.0.1:9931/v1`, sending the key `llama-server` was started with (`--api-key`) from the `MENLO_AI_API_KEY` secret. Before the first deploy:

- Turn `mmproj-auto` on for the `qwen3.5-4b` preset (vision needs it)
- Raise `--models-max` only if loading the chat model next to the knowledge base's models needs it

The endpoint and the model names (`MENLO_AI_ENDPOINT`, `MENLO_AI_CHAT_MODEL`, `MENLO_AI_VISION_MODEL`) are set once at the top of `cd.yml`; change the model there.

If `llama-server` is stopped the app still runs: `/api/ai/health` returns a problem response and the AI features fail, while budgets and planning keep working.

## 🚢 Deployment

### Production Deployment (via GitHub Actions)

The image is built once, in CI, and CD deploys that exact image:

1. **CI** (`ci.yml`, `Container Image` job) builds the `linux/arm64` image on every PR to check the `Dockerfile`. On `main` it also pushes `ghcr.io/digibanks99/menlo/menlo-api:sha-<full commit sha>`. It only builds when the API, SPA, `Dockerfile` or pipeline files change.
2. **CD** (`cd.yml`) starts when CI succeeds on `main`. It looks up `sha-<commit>`; if CI built no image for that commit (e.g. docs-only changes), the deploy is skipped. Otherwise it:
   1. Downloads the Entra client certificate from Key Vault
   2. Writes `~/menlo/docker-compose.prod.yml` on the host and restarts the stack via the self-hosted runner
   3. Verifies the local health check (the job fails if `/health` never returns 200)
   4. Restarts the Cloudflare tunnel (if configured)
   5. Smoke-checks `FRONTEND_URL` from outside with [`smoke-check.sh`](smoke-check.sh) (`/health` 200, `/api/budgets` 401 without auth, `/` and a deep link return the SPA with a CSP header), retrying for up to 3 minutes
   6. Moves the `main` tag to the deployed image only if the smoke check passed, so `menlo-api:main` is always the last image that passed

If the smoke check fails, the `main` tag still points at the previous image, so `gh workflow run cd.yml --ref main` rolls back. Run the smoke check yourself with `deployment/smoke-check.sh https://menlo.yourdomain.com`.

CD never builds images, and only one production deploy runs at a time.

### Redeploying and Rolling Back

Redeploy the image currently in production (no SHA):

```bash
gh workflow run cd.yml --ref main
```

Roll back (or forward) to the image CI built for a specific commit on `main`:

```bash
gh workflow run cd.yml --ref main -f sha=$(git rev-parse <commit>)
```

The SHA must be the full 40 characters, and that commit must have an image (`docker buildx imagetools inspect ghcr.io/digibanks99/menlo/menlo-api:sha-<sha>`). Rolling back the code doesn't roll back database migrations.

To restart the current stack on the host without a new image:

```bash
sudo -iu ghrunner
cd ~/menlo
docker compose -f docker-compose.prod.yml up -d
```

## 🔁 Certificate rotation

The prod API signs in to Entra (`app-menlo-prod`) with the certificate `menlo-cd-cert` in `kv-menlo-prod`. It is a self-signed 12-month certificate, and Entra only trusts the public keys uploaded to the app registration, so renewing it in Key Vault alone would break sign-in. The [`Cert Rotation`](../.github/workflows/cert-rotation.yml) workflow does the whole sequence:

1. Runs daily at 05:17 SAST (03:17 UTC) and reads the certificate expiry.
2. If it expires in `ROTATE_WITHIN_DAYS` (30) days or fewer, or the run was forced, it creates a new Key Vault version from the current policy.
3. Adds the new public key to `app-menlo-prod` (`--append`, so the old key stays trusted) and waits two minutes for Entra to pick it up.
4. Dispatches `cd.yml` on `main`, which downloads the latest certificate and redeploys the current image, and waits for it to pass its smoke check.
5. Only after that, removes keys past their expiry from `app-menlo-prod`.

If any step fails, the workflow opens a GitHub issue labelled `cert-rotation` (or comments on the open one). Sign-in keeps working through a failed run, because the running container still has the old certificate and the old key is still trusted; re-running converges.

### Forcing a rotation

```bash
gh workflow run cert-rotation.yml --ref main -f force=true
gh run watch
```

### Things to know

- The workflow only runs from `main`, in the `Production` environment, using the `app-menlo-build` federated credential. That identity must own `app-menlo-prod` and `app-menlo-nonprod` (Graph `Application.ReadWrite.OwnedBy`). It also needs the `Key Vault Certificates Officer` role on `kv-menlo-prod` to create new certificate versions; `infra/modules/secrets/main.bicep` grants it, so run `CD - Infra` once after it merges.
- GitHub disables scheduled workflows after 60 days without repository activity. Check that `Cert Rotation` is enabled in the Actions tab, or run `gh workflow enable cert-rotation.yml`. The Key Vault `EmailContacts` action (30 days before expiry) is the backstop.
- Key Vault `AutoRenew` is deliberately not used. See [ADR-003](../docs/decisions/adr-003-cd-authentication-strategy.md).

## 🏥 Health Monitoring

Run these as the runner user (`sudo -iu ghrunner`) from `~/menlo`. The examples use `docker`; with Podman, the same commands work with `podman` in place of `docker`.

### Check Service Status

```bash
docker compose -f docker-compose.prod.yml ps
```

### View Logs

```bash
# All services
docker compose -f docker-compose.prod.yml logs -f

# Specific service
docker compose -f docker-compose.prod.yml logs -f menlo-api
```

### Health Check URLs

- **API Health**: `http://localhost:8080/health`
- **API Documentation**: `http://localhost:8080/swagger`
- **llama.cpp**: `http://127.0.0.1:9931/v1/models`
- **AI health (signed in)**: `https://menlo.yourdomain.com/api/ai/health`
- **External (via Cloudflare)**: `https://menlo.yourdomain.com/health`, or run `deployment/smoke-check.sh https://menlo.yourdomain.com`

## 🤖 AI Models

Models belong to the host's `llama-server` preset, not to the Menlo stack. Check what it serves:

```bash
curl -H "Authorization: Bearer $LLAMA_API_KEY" http://127.0.0.1:9931/v1/models
```

## 🗄️ Database Backup & Restore

The reference [`docker-compose.prod.yml`](docker-compose.prod.yml) defines a `db-backup` service using [`prodrigestivill/postgres-backup-local`](https://github.com/prodrigestivill/docker-postgres-backup-local). It writes daily backups to the `menlo_postgres_backups` volume and keeps the last 7.

> **Note:** The compose file generated by the CD workflow does not include `db-backup` yet. The commands below apply when the backup service is running.

### Verify a Backup Ran

```bash
# List backup files in the volume
docker exec menlo-db-backup ls -lh /backups/daily/
```

You should see `.sql.gz` files named with a date (e.g., `menlo-20260417.sql.gz`) and a `menlo-latest.sql.gz` symlink pointing to the most recent one.

### Restore from a Backup

```bash
# Copy the latest backup out of the volume (or use a specific dated file)
docker cp menlo-db-backup:/backups/last/menlo-latest.sql.gz .

# Decompress
gunzip menlo-latest.sql.gz

# Stop the API first to avoid conflicts
docker stop menlo-api

# Restore into the running postgres container
docker exec -i menlo-postgres psql -U menlo -d menlo < menlo-latest.sql

# Restart the API
docker start menlo-api
```

> **Note:** To restore a specific point-in-time backup, replace `menlo-latest.sql.gz` with a timestamped file from `/backups/last/` (e.g., `menlo-20260417-020000.sql.gz`).

### Configure Schedule and Retention

Set these environment variables before bringing the stack up (e.g., in a `.env` file):

| Variable | Default | Description |
|---|---|---|
| `BACKUP_SCHEDULE` | `@daily` | Cron expression or `@daily`/`@hourly` shorthand |
| `BACKUP_KEEP_DAYS` | `7` | Number of daily backups to retain |

Example `.env` override:

```env
BACKUP_SCHEDULE=0 2 * * *
BACKUP_KEEP_DAYS=14
```

## 🛠️ Troubleshooting

### Deploy Job Fails Immediately

- `pwsh: command not found`: the workflow is older than the bash migration ([#516](https://github.com/DigiBanks99/menlo/issues/516)); rebase onto `main`
- `Neither podman nor docker is installed` / `CONTAINER_RUNTIME is '...' but it is not installed`: install the runtime (Step 1) or fix the variable
- `permission denied ... docker.sock`: add the runner user to the `docker` group and restart the runner service
- `podman compose` errors about a missing provider: install `podman-compose`
- `no matching manifest for linux/arm64/v8`: the image was built for a different architecture; `menlo-api` is only built for `linux/arm64`, so an `amd64` host (`uname -m` prints `x86_64`) needs that platform added to the `Container Image` job in `ci.yml`

### Container Runtime Issues

```bash
# Docker
docker info
systemctl status docker

# Podman
podman info
# Containers not coming back after a reboot
loginctl show-user ghrunner --property=Linger
systemctl --user status podman-restart.service
```

### Database Issues

```bash
# Reset database (WARNING: destroys data)
docker compose -f docker-compose.prod.yml down
docker volume rm menlo_postgres_data
gh workflow run cd.yml --ref main

# Check database connection
docker exec menlo-postgres pg_isready -U menlo -d menlo
```

### AI Issues

```bash
# Is llama-server up, and does it serve the configured model?
curl -H "Authorization: Bearer $LLAMA_API_KEY" http://127.0.0.1:9931/v1/models
```

The API reaches it over the host loopback, so check `llama-server` on the host rather than inside a container.

If `/api/ai/health` reports `HTTP 401 ... Invalid API Key`, the `MENLO_AI_API_KEY` secret doesn't match the `--api-key` `llama-server` was started with. Fix the secret and redeploy.

### Removing the Old Ollama Data

Earlier deploys ran an Ollama container. The next deploy removes the `menlo-ollama` container (`up --remove-orphans`), but not its volume. Once the new deploy is healthy, free the space by hand:

```bash
docker volume rm menlo_ollama_data
```

### Network/Cloudflare Issues

```bash
# Check tunnel status
cloudflared tunnel info menlo-api
systemctl status cloudflared
journalctl -u cloudflared -n 100

# Restart tunnel service
sudo systemctl restart cloudflared

# Test local connectivity
curl http://localhost:8080/health

# Test external connectivity (replace with your domain)
curl https://YOUR_SUBDOMAIN.YOUR_DOMAIN/health
```

## 📊 Resource Requirements

### Minimum System Requirements

- **CPU**: 4 cores
- **RAM**: 16GB (shared with the host's `llama-server` models)
- **Storage**: 100GB free space
- **Network**: Stable broadband

### Expected Resource Usage

| Component | CPU | Memory | Storage |
|-----------|-----|--------|---------|
| Menlo API | 1-2 cores | 512MB | 1GB |
| PostgreSQL | 1 core | 256MB | 5-10GB |
| **Total (excluding llama-server)** | **2-3 cores** | **~1GB** | **6-11GB** |

### Performance Notes

- **Startup**: ~30 seconds to full readiness
- **AI inference**: 2-10 seconds per request (depending on model and query)

## 🌐 Frontend Hosting

The Angular SPA is not deployed separately. The root `Dockerfile` builds it in its `spa-build` stage and copies it into the API's `wwwroot`. The API then serves the SPA and `/api` from one origin (`FRONTEND_URL`), falling back to `index.html` for client-side routes. Deploying the API image deploys the frontend.

Cloudflare Pages hosted the frontend until 2026; [ADR-005](../docs/decisions/adr-005-bundled-spa-hosting.md) records why it was retired.

To run the production image locally, see `Menlo__BundledUi` in `AGENTS.md`.

## 🔐 Security Considerations

### Network Security

- llama.cpp and PostgreSQL are not exposed externally; PostgreSQL is published on `127.0.0.1:5432` only
- Only Menlo API exposed via Cloudflare Tunnel
- Self-hosted GitHub Actions runner provides secure deployment access
- Host firewall (e.g. `ufw`/`firewalld`) should block inbound 5432, 8080 and 9931; the tunnel connects outbound only
- The API container uses host networking (as the knowledge base does), so it reaches `llama-server` on loopback without opening it to containers. It listens on `127.0.0.1:8080` only (`ASPNETCORE_URLS`), so any host-port clash shows up as a failed start
- `X-Forwarded-For`/`X-Forwarded-Proto` are trusted only from loopback (`ForwardedHeaders__KnownNetworks__0=127.0.0.0/8`), where cloudflared's requests arrive. A wrong value makes sign-in fail with AADSTS500112. Do not set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`; it makes the framework trust every source

### Data Security

- All data remains local (no cloud databases)
- AI processing happens locally (no external AI APIs)
- `~/menlo/docker-compose.prod.yml` contains database credentials and the Entra certificate; the deploy job writes it with owner-only permissions

### Access Control

- Cloudflare Access can be configured for additional authentication
- MFA recommended for Cloudflare account
- The runner user only needs passwordless `sudo` for `systemctl restart cloudflared`

## 📞 Support

For issues specific to this deployment:

1. **Check logs** using the commands above
2. **Review GitHub Actions** logs for the `Deploy to Home Server` job
3. **Test connectivity** using health check URLs

For general Menlo application issues, see the main [documentation](../docs/README.md).

---

*This deployment implements the cost-conscious, privacy-first architecture defined in [ADR-001](../docs/decisions/adr-001-hosting-strategy.md) on the Linux host described in [ADR-004](../docs/decisions/adr-004-linux-deployment-host.md).*
