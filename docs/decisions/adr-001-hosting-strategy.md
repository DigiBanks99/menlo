# ADR-001: Hosting Strategy for Home Management Application

## Status

**Revised** - 2025-11-29 (Original accepted 2025-06-01)

## Revision Summary (2025-11-29)

The original decision selected Azure Static Web Apps for frontend hosting. This has been **revised** to use **Cloudflare Pages** for the following reasons:

- Consolidation: We already rely on Cloudflare Tunnel for secure backend access; using Pages unifies edge delivery and simplifies DNS/domain management.
- Domain Alignment: Single apex/root domain `menlo.wilcob.co.za` now serves both UI and API (`/api` via tunnel) eliminating cross-origin deployment complexity.
- Modern Frontend Needs: Angular upgraded to **v21**; Cloudflare Pages supports flexible build commands and edge asset caching without Azure-specific integration.
- Simplicity & Cost: Removes Azure Static Web Apps configuration surface; Cloudflare Pages free tier suffices for single-family usage.
- Tighter CORS Control: API now restricts production origin to `https://menlo.wilcob.co.za` (requirement tracked separately).

### New Decision

**Selected (Revised)**: Hybrid Architecture with **Cloudflare Pages** + **Cloudflare Tunnel**

- **Frontend**: Cloudflare Pages (Angular 21 PWA)
- **Backend + AI**: Home server accessed via Cloudflare Tunnel
- **Database**: PostgreSQL on home server
- **AI Processing**: Local Ollama with Phi models
- **Connectivity**: Cloudflare Tunnel (eliminates static IP requirement)

All references to Azure Static Web Apps in architecture, requirements and diagrams must be updated.
This work is tracked by requirement `cloudflare-pages-frontend`.
The historical evaluation below is retained for context.

## Context

The Home Management Application requires a hosting strategy that balances cost-consciousness, privacy requirements for AI processing, and the experimental nature of the project. Key considerations:

- **Cost Constraints**: This is a conceptual application with uncertain adoption and ROI
- **Local AI Requirements**: Ollama + Phi models for privacy and cost control
- **Family Usage**: Single family of 5 with predictable, low-scale usage patterns
- **South African Context**: Local internet infrastructure and pricing considerations
- **Experimental Nature**: Willingness to sacrifice some performance for cost savings during validation phase

## Original Decision (Historic Reference)

**Selected (Historic)**: Hybrid Architecture with Azure Static Web Apps + Cloudflare Tunnel

- (Historic) **Frontend**: Azure Static Web Apps (Free tier)
- **Backend + AI**: Home server accessed via Cloudflare Tunnel
- **Database**: PostgreSQL on home server
- **AI Processing**: Local Ollama with Phi models
- **Connectivity**: Cloudflare Tunnel (eliminates static IP requirement)

## Options Considered

### Option 1: Hybrid Architecture (Historic Selected)

**Architecture:**

```sh
[PWA/Web] → [Azure Static Web Apps] → [Cloudflare Tunnel] → [Home Server API] → [Local Ollama AI]
                                                                    ↓
                                                            [Local PostgreSQL]
```

**Pros (Historic):**

- ✅ Very low ongoing costs (~R150-250/month)
- ✅ UI globally distributed via Azure CDN
- ✅ Complete AI privacy (no external AI service costs)
- ✅ Full control over AI models and capabilities
- ✅ Zero vendor lock-in for core data and AI
- ✅ Can start immediately with existing hardware
- ✅ **No static IP required** - Cloudflare Tunnel handles connectivity
- ✅ **Enhanced security** - no open ports on home router
- ✅ **Built-in SSL/DDoS protection** via Cloudflare
- ✅ **Custom domain support** with professional appearance
- ✅ Suitable for experimental/validation phase

**Cons (Historic):**

- ❌ Depends on home internet reliability
- ❌ Single point of failure for backend
- ❌ Manual backup and maintenance responsibility
- ❌ Dependency on Cloudflare service availability

**Estimated Costs (ZAR/month):**

- Azure Static Web Apps: R0 (Free tier)
- Cloudflare Tunnel: R0 (Free tier)
- Custom domain: ~R15/month (R180/year)
- Home server electricity: ~R150
- UPS/Infrastructure: ~R100-200
- **Total: ~R165-365/month** (significantly lower than static IP option)

### Option 2: Full Azure Cloud

**Architecture:**

```sh
[PWA] → [Azure Container Apps] → [Azure SQL/Cosmos] 
                ↓
        [Azure OpenAI/Cognitive Services]
```

**Pros:**

- ✅ Professional reliability and uptime
- ✅ Automatic scaling and redundancy
- ✅ Managed backups and security
- ✅ No home infrastructure dependencies
- ✅ Global accessibility

**Cons:**

- ❌ High monthly costs (~R1500-3000+/month)
- ❌ AI service costs per token/request
- ❌ Vendor lock-in
- ❌ Data privacy concerns with external AI
- ❌ Overkill for single family usage
- ❌ High upfront investment for experimental project

**Estimated Costs (ZAR/month):**

- Azure Container Apps: ~R800-1500
- Azure SQL Database: ~R500-1000
- Azure OpenAI: ~R200-500 (usage-based)
- Storage/Networking: ~R200
- **Total: ~R1700-3200/month**

### Option 3: Fully Local with VPN

**Architecture:**

```sh
[Home Network] → [Local Server] → [Local AI + Database]
        ↓
[VPN/Tailscale for remote access]
```

**Pros:**

- ✅ Lowest possible costs (~R150/month electricity)
- ✅ Complete data sovereignty
- ✅ No internet dependency for local use
- ✅ Maximum privacy

**Cons:**

- ❌ No external access without VPN setup
- ❌ Poor mobile experience
- ❌ Family members need VPN configuration
- ❌ Limited to home network primarily

**Estimated Costs (ZAR/month):**

- Electricity: ~R150
- VPN service (optional): ~R100
- **Total: ~R150-250/month**

### Option 4: Hybrid with Cloudflare Tunnel (Historic Alternative Consideration)

**Architecture:**

```sh
[PWA/Web] → [Azure Static Web Apps] → [Cloudflare Edge] → [Cloudflare Tunnel] → [Home Server API] → [Local Ollama AI]
                                                                                        ↓
                                                                                [Local PostgreSQL]
```

**Pros (Historic):**

- ✅ **Eliminates static IP requirement entirely**
- ✅ **Enhanced security** - no open ports on home router
- ✅ **Built-in SSL certificates** and DDoS protection
- ✅ **Professional custom domain** setup
- ✅ **Global edge network** - Cloudflare has presence in Johannesburg
- ✅ Very low ongoing costs (~R165-365/month)
- ✅ Complete AI privacy and control
- ✅ Works with any internet connection (fiber, LTE, etc.)
- ✅ Automatic reconnection and tunnel management

**Cons (Historic):**

- ❌ Dependency on Cloudflare service availability
- ❌ Additional service to manage (though minimal)
- ❌ Still depends on home internet reliability
- ❌ Manual backup and maintenance responsibility

**Estimated Costs (ZAR/month):**

- Azure Static Web Apps: R0 (Free tier)
- Cloudflare Tunnel: R0 (Free tier)
- Custom domain: ~R15/month (if not already owned)
- Home server electricity: ~R150
- UPS/Infrastructure: ~R100-200
- **Total: ~R165-365/month**

### Option 5: Serverless with External AI

**Architecture:**

```sh
[Static Site] → [Azure Functions] → [External AI APIs]
                     ↓
              [Azure SQL Serverless]
```

**Pros:**

- ✅ Pay-per-use pricing model
- ✅ No server management
- ✅ Good for sporadic usage

**Cons:**

- ❌ AI costs scale with usage
- ❌ Cold start latency issues
- ❌ Complex state management
- ❌ Still significant monthly costs for family usage

## Implementation Plan

### Phase 1: MVP Validation (Historic)

1. **Frontend**: (Historic) Deploy Angular PWA to Azure Static Web Apps (Free tier)

### Revision Implementation Plan (Cloudflare Pages)

1. **Angular Upgrade**: Upgrade UI to Angular 21 (signals, deferred views, latest CLI) – see requirement `cloudflare-pages-frontend`.
2. **Cloudflare Pages Project**: Connect GitHub repo; configure build command `pnpm install && pnpm exec ng build --configuration=production`.
3. **Routing / SPA**: Enable single-page application fallback (`_redirects` or Pages config) to `index.html`.
4. **Domain Integration**: Point `menlo.wilcob.co.za` to Pages; ensure `/api/*` requests route through existing tunnel without origin mismatch.
5. **CORS Hardening**: Update API to allow origin only `https://menlo.wilcob.co.za` in production; keep localhost origins for development.
6. **Cache Strategy**: Use Pages edge caching for static assets with immutable hashes; confirm PWA offline for core budget/list features.
7. **Monitoring**: Add Pages deployment status checks to CI and document rollback (redeploy previous build / pin commit).
8. **Docs Update**: Replace Azure Static Web Apps references across README, architecture, diagrams and requirements.

### Updated Success Metrics

- Seamless single-domain experience (no CORS preflight for primary UI/API interactions)
- Frontend redeploy time < 2 minutes
- Cache hit ratio for static assets > 90%
- Angular 21 features (signals, improved build performance) enabled and documented

## Risks and Mitigations

| Risk                      | Impact | Likelihood | Mitigation                                   |
| ------------------------- | ------ | ---------- | -------------------------------------------- |
| Home internet outage      | High   | Medium     | UPS for router, mobile hotspot backup        |
| Hardware failure          | High   | Low        | Regular backups, spare hardware plan         |
| Cloudflare service outage | Medium | Low        | Minimal risk, enterprise-grade service       |
| Family adoption too low   | High   | Medium     | This validates not investing heavily upfront |
| Performance issues        | Medium | Medium     | Monitor and optimize, cloud migration option |

## Success Metrics

- **Cost Target**: <R500/month total hosting costs
- **Availability Target**: >95% uptime for family usage patterns
- **Performance Target**: <2 second API response times
- **User Satisfaction**: Wife actively uses planning features

## Notes

**Cloudflare Tunnel Setup**:

- Eliminates need for static IP or port forwarding
- Provides enterprise-grade security and performance
- Custom domain setup requires DNS configuration
- Free tier suitable for family usage patterns

**Alternative Connectivity Options** (if Cloudflare Tunnel not suitable):

- Dynamic DNS services (DuckDNS, No-IP)
- VPN solutions (Tailscale, ZeroTier)
- Traditional static IP from ISPs

**Hardware Requirements for Home Server:**

- Minimum: 8GB RAM, 4-core CPU, 500GB SSD
- Recommended: 16GB RAM, 6-8 core CPU, 1TB SSD
- Can start with existing desktop/laptop hardware

## Review Date

This decision should be reviewed after 6 months of operation (December 2025) to evaluate:

- Actual costs vs projections
- Family adoption and usage patterns
- Technical performance and reliability
- Whether to continue hybrid or migrate to cloud
