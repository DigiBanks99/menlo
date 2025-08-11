# Cloudflare Tunnel - Network Flow Diagram

```mermaid
flowchart TD
    User([Remote User])
    CloudflareEdge([Cloudflare Edge])
    Tunnel(["Cloudflare Tunnel (cloudflared)"])
    MenloServices([Menlo Local Services])

    User -- HTTPS --> CloudflareEdge
    CloudflareEdge -- Encrypted Tunnel --> Tunnel
    Tunnel -- Local HTTP/TCP --> MenloServices

    subgraph Home Network
        Tunnel
        MenloServices
    end
```

This diagram shows how remote users securely access Menlo's local services via Cloudflare Tunnel, with all traffic encrypted and no inbound ports required.
