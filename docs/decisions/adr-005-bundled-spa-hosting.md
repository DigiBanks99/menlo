# ADR-005: Bundled SPA Hosting

## Status

**Accepted** - 2026-10-31

Supersedes the Cloudflare Pages frontend in [ADR-001](adr-001-hosting-strategy.md) and the line in [ADR-004](adr-004-linux-deployment-host.md) that kept it.

## Context

ADR-001 hosted the Angular frontend on Cloudflare Pages, and the API on the home server behind Cloudflare Tunnel. Two things have since happened:

- **The API image serves the SPA.** The root `Dockerfile` builds the SPA in its `spa-build` stage and copies it into the API's `wwwroot`. The API serves it with an `index.html` fallback for client-side routes. Production at `FRONTEND_URL` serves `/`, `/api/*` and `/health` from that one container, with the production Content Security Policy set by ASP.NET.
- **The Pages deploy has been broken since 2026-05-08.** The frontend deploy workflow failed on every UI change: `wrangler-action` installs `wrangler@4`, and pnpm rejects `workerd`'s build script (`ERR_PNPM_IGNORED_BUILDS`). It left a red check on `main` that shipped nothing.

## Options Considered

### 1. Fix the Pages deploy

- **Pros**: Static assets from Cloudflare's edge
- **Cons**: Two deploys of one app that can drift apart; the frontend and API on different origins again (CORS, cookie and CSP headers in two places); a second set of Cloudflare credentials in GitHub
- **Assessment**: Edge caching doesn't matter for one family on a home connection, and the bundled SPA already works

### 2. Serve the SPA from the API image (Selected)

- **Pros**: One artifact and one deploy; frontend and API always match; same origin, so no CORS; one place sets the security headers; the deploy smoke check covers the SPA too
- **Cons**: The SPA is served from the home server rather than Cloudflare's edge, so static assets travel through the tunnel; a UI-only change rebuilds and redeploys the API image
- **Assessment**: Simplest setup that matches what production already does

## Decision

The API image bundles and serves the Angular SPA. There is no separate frontend hosting.

- `cd.yml` (renamed from the backend CD workflow) is the only deploy workflow. It deploys the API image, and with it the SPA
- The frontend deploy workflow, its Storybook-to-GitHub-Pages job, CI's Pages artifact and the Pages-only `_redirects` file are removed. Storybook is still built in CI but not published
- The Cloudflare Pages project and its GitHub secrets and variables (`CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `CLOUDFLARE_PAGES_PROJECT_NAME`), and the Storybook `STORYBOOK_URL` variable, are retired

Cloudflare Tunnel, the Linux host (ADR-004) and certificate-based Entra authentication (ADR-003) are unchanged.

## Consequences

### Positive

- No red CD runs from a deploy that ships nothing
- One deploy, and one rollback (`gh workflow run cd.yml --ref main -f sha=<sha>`) restores the API and the UI together
- No cross-origin setup between frontend and API

### Negative

- Static assets are not cached at Cloudflare's edge unless Cloudflare caching rules are added for them
- Every UI change produces a new API image and a production deploy

## Related Documents

- [ADR-001: Hosting Strategy](adr-001-hosting-strategy.md)
- [ADR-004: Linux Deployment Host](adr-004-linux-deployment-host.md)
- [Deployment guide](../../deployment/README.md)
