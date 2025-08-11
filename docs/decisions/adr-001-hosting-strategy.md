# ADR-001: Hosting Strategy for Home Management Application

## Status

**Accepted** - 2025-06-01

## Context

The Home Management Application requires a hosting strategy that balances cost-consciousness, privacy requirements for AI processing, and the experimental nature of the project. Key considerations:

- **Cost Constraints**: This is a conceptual application with uncertain adoption and ROI
- **Local AI Requirements**: Ollama + Phi models for privacy and cost control
- **Family Usage**: Single family of 5 with predictable, low-scale usage patterns
- **South African Context**: Local internet infrastructure and pricing considerations
- **Experimental Nature**: Willingness to sacrifice some performance for cost savings during validation phase

## Decision

**Selected**: Hybrid Architecture with Azure Static Web Apps + Cloudflare Tunnel

- **Frontend**: Azure Static Web Apps (Free tier)
- **Backend + AI**: Home server accessed via Cloudflare Tunnel
- **Database**: PostgreSQL on home server
- **AI Processing**: Local Ollama with Phi models
- **Connectivity**: Cloudflare Tunnel (eliminates static IP requirement)

## Options Considered

### Option 1: Hybrid Architecture (SELECTED)

**Architecture:**

```sh
[PWA/Web] → [Azure Static Web Apps] → [Cloudflare Tunnel] → [Home Server API] → [Local Ollama AI]
                                                                    ↓
                                                            [Local PostgreSQL]
```

**Pros:**

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

**Cons:**

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

### Option 4: Hybrid with Cloudflare Tunnel (ALTERNATIVE CONSIDERATION)

**Architecture:**

```sh
[PWA/Web] → [Azure Static Web Apps] → [Cloudflare Edge] → [Cloudflare Tunnel] → [Home Server API] → [Local Ollama AI]
                                                                                        ↓
                                                                                [Local PostgreSQL]
```

**Pros:**

- ✅ **Eliminates static IP requirement entirely**
- ✅ **Enhanced security** - no open ports on home router
- ✅ **Built-in SSL certificates** and DDoS protection
- ✅ **Professional custom domain** setup
- ✅ **Global edge network** - Cloudflare has presence in Johannesburg
- ✅ Very low ongoing costs (~R165-365/month)
- ✅ Complete AI privacy and control
- ✅ Works with any internet connection (fiber, LTE, etc.)
- ✅ Automatic reconnection and tunnel management

**Cons:**

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

### Phase 1: MVP Validation (0-6 months)

1. **Frontend**: Deploy Angular PWA to Azure Static Web Apps (Free tier)
2. **Backend**: .NET API on home server (existing hardware)
3. **Database**: PostgreSQL on same home server
4. **AI**: Ollama with Phi-3.5-mini locally
5. **Connectivity**: Set up Cloudflare Tunnel with custom domain
6. **Setup Steps**:
   - Install `cloudflared` on home server
   - Create tunnel: `cloudflared tunnel create menlo-api`
   - Configure tunnel to point to local API (localhost:5000)
   - Set up custom domain DNS records
   - Configure tunnel as system service

### Phase 2: Optimization (6-12 months)

1. **Performance**: Monitor latency and reliability
2. **Security**: Implement proper SSL, firewall rules
3. **Backup**: Automated backup strategy
4. **AI Enhancement**: Upgrade to larger Phi models if needed

### Phase 3: Scale Decision (12+ months)

1. **Usage Analysis**: Evaluate actual family adoption
2. **Cost Review**: Compare actual costs vs cloud alternatives
3. **Migration Decision**: Stay hybrid or move to cloud based on success

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

---

*This ADR reflects the experimental nature of the project and prioritizes learning and validation over enterprise-grade reliability.*
