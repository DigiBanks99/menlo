# Cloudflare Tunnel Requirements - Specifications

## Context

Cloudflare Tunnel will be used to securely expose Menlo's local backend services (e.g., APIs, admin interfaces) to the public internet without opening inbound firewall ports. This supports Menlo's hybrid cloud-local architecture, enabling secure remote access and integration with cloud-hosted frontends, while maintaining privacy and resilience for local services.

## Business Requirements

- **Secure Remote Access:**
  - The system must allow authorized users to access local Menlo services remotely, without exposing the home network to the public internet.

- **Privacy Preservation:**
  - All traffic between Cloudflare and the local Menlo instance must be encrypted end-to-end.

- **Zero Trust Principle:**
  - Access to local services must be protected by authentication and authorization, leveraging Cloudflare Access or similar mechanisms.

- **High Availability:**
  - The tunnel must automatically reconnect and recover from local network interruptions or power outages.
  - Support for automated restart through CI/CD pipeline when deployments occur.

- **Auditability:**
  - All remote access events must be logged for security and troubleshooting purposes.
  - CI/CD deployment activities related to tunnel management must be logged and traceable.

- **Ease of Use:**
  - The setup and maintenance of the tunnel should require minimal technical expertise from the end user.
  - Integration with CI/CD pipeline should provide automated tunnel management without manual intervention.

## Technical Requirements

- **Protocol & Security:**
  - Use Cloudflare Tunnel (cloudflared) to establish outbound-only connections from the local Menlo server to Cloudflare's edge.
  - All connections must use TLS.

- **Authentication:**
  - Integrate with Cloudflare Access for identity-based access control (e.g., Google, Microsoft, GitHub SSO).
  - Support for multi-factor authentication (MFA) for remote access.

- **Service Exposure:**
  - Allow configuration of which local services (ports, hostnames) are exposed via the tunnel.
  - Support both HTTP(S) and TCP tunnels as needed.

- **Resilience:**
  - The tunnel service must run as a system service (e.g., systemd, Windows service) and auto-restart on failure.
  - Only a single tunnel instance is required; redundancy is not needed for this home solution.

- **Monitoring & Logging:**
  - Expose health and status metrics for the tunnel (e.g., via Prometheus or logs).
  - Log all connection attempts, successes, and failures.

- **Configuration Management:**
  - Tunnel configuration must be manageable in a version-controlled, human-readable format (e.g., YAML), or via C#-native configuration if supported.
  - If possible, support direct configuration through C# code or .NET configuration providers, in addition to YAML.
  - Support for automated deployment and updates via CI/CD pipeline using self-hosted GitHub Actions runner.

- **CI/CD Integration:**
  - Tunnel service management (start, stop, restart, configuration updates) must be integrated into the CI/CD pipeline.
  - Use self-hosted GitHub Actions runner with direct line-of-sight to local services, eliminating the need for SSH-based deployments.
  - Support automated tunnel service restart as part of the application deployment process.
  - Ensure tunnel configuration updates can be deployed through the standard CI/CD workflow.

- **Firewall Compatibility:**
  - The solution must work behind NAT and firewalls, requiring only outbound HTTPS (port 443) connectivity.

- **Scalability:**
  - Support for exposing additional services in the future without major reconfiguration.

## Non-Requirements

- No explicit budget or time constraints.
- No requirement for public IP addresses or direct inbound port forwarding.

---

See also: [Implementation Plan](implementation.md), [Test Cases](test-cases.md), [Diagrams](diagrams/).
