# Cloudflare Tunnel - Test Cases

Each test case below maps to a business or technical requirement from the specifications. All tests are written to be clear, actionable, and verifiable.

## Business Requirements

- [ ] **Secure Remote Access:**
  - Verify that authorized users can access local Menlo services remotely via the tunnel.
  - Verify that unauthorized users are denied access.

- [ ] **Privacy Preservation:**
  - Verify that all tunnel traffic is encrypted end-to-end (TLS).

- [ ] **Zero Trust Principle:**
  - Verify that access to local services requires authentication and authorization via Cloudflare Access.
  - Verify that access can be restricted by the Microsoft Entra identity provider.

- [ ] **High Availability:**
  - Simulate a local network interruption and verify the tunnel automatically reconnects.
  - Simulate a power outage and verify the tunnel recovers on restart.

- [ ] **Auditability:**
  - Verify that all remote access events are logged with timestamp, user, and action.

- [ ] **Ease of Use:**
  - Verify that a non-technical user can follow setup instructions and establish a working tunnel.

## Technical Requirements

- [ ] **Protocol & Security:**
  - Verify that only outbound connections are required (no inbound firewall rules).
  - Verify that the tunnel uses TLS for all connections.

- [ ] **Authentication:**
  - Verify integration with Cloudflare Access for SSO.
  - Verify that MFA can be enforced for remote access.

- [ ] **Service Exposure:**
  - Verify that specific local services (ports/hostnames) can be selectively exposed.
  - Verify support for both HTTP(S) and TCP tunnels.

- [ ] **Resilience:**
  - Verify the tunnel runs as a system service and auto-restarts on failure.
  - Verify support for multiple simultaneous tunnels.

- [ ] **Monitoring & Logging:**
  - Verify that health/status metrics are available (e.g., via Prometheus or logs).
  - Verify that all connection attempts, successes, and failures are logged.

- [ ] **Configuration Management:**
  - Verify that tunnel configuration is stored in a version-controlled, human-readable format (YAML).
  - Verify that configuration can be updated and deployed automatically.

- [ ] **Firewall Compatibility:**
  - Verify that the tunnel works behind NAT/firewalls with only outbound HTTPS (port 443) open.

- [ ] **Scalability:**
  - Verify that additional services can be exposed via the tunnel without major reconfiguration.

---

See also: [Specifications](specifications.md), [Implementation Plan](implementation.md), [Diagrams](diagrams/).
