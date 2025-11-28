# Cloudflare Tunnel - Implementation Plan

This plan provides a stepwise approach for deploying and managing Cloudflare Tunnel for Menlo, ensuring all business and technical requirements are met.

## CI/CD Integration Approach

The Menlo project uses a **self-hosted GitHub Actions runner** approach for deployments, eliminating the need for SSH-based remote access. This approach provides:

### Architecture Benefits

- **Direct Line-of-Sight**: Self-hosted runner operates on the same network as the Menlo services
- **Simplified Security**: No SSH key management or remote access configuration required
- **Branch Protection**: GitHub's branch protection rules provide deployment security
- **Audit Trail**: All deployment activities are logged in GitHub Actions

### Deployment Flow

1. **Build Phase**: Container images are built and pushed to GitHub Container Registry on shared runners
2. **Deploy Phase**: Self-hosted runner pulls images and deploys directly to local infrastructure
3. **Tunnel Management**: Cloudflare tunnel service is restarted automatically as part of deployment
4. **Verification**: Health checks are performed locally on the self-hosted runner

### Security Considerations

- Self-hosted runner should be properly secured and maintained
- Access to the runner should be restricted to necessary personnel
- Regular security updates should be applied to the runner environment
- Network isolation should be maintained between the runner and external networks

---

## Steps

1. **Preparation**
   - Review specifications and test cases.
   - Identify local services to be exposed (APIs, admin interfaces, etc.).
   - Ensure outbound HTTPS (port 443) is open on the local network.
   - Set up self-hosted GitHub Actions runner with proper security configuration.

2. **Cloudflare Account & Access Setup**
   - Create or use an existing Cloudflare account.
   - Set up Cloudflare Access with required identity providers (Google, Microsoft, GitHub, etc.).
   - Configure access policies and MFA requirements.

3. **Install cloudflared**
   - Download and install the `cloudflared` binary on the Menlo server (Windows/Linux/Mac).
   - Register the tunnel with Cloudflare and obtain credentials.

4. **Configure Tunnel**
   - Create a YAML configuration file specifying local services to expose.
   - Store configuration in version control.
   - Test configuration locally.

5. **Run as a Service & CI/CD Integration**
   - Set up `cloudflared` as a system service (systemd, Windows service, etc.).
   - Enable auto-restart on failure.
   - Configure CI/CD pipeline (GitHub Actions) with self-hosted runner for automated deployments.
   - Integrate tunnel service management into deployment workflow:
     - Restart tunnel service automatically after application deployments.
     - Support tunnel configuration updates through version control.
     - Ensure tunnel service health is verified as part of deployment verification.

6. **Monitoring & Logging**
   - Enable logging of all access events and connection attempts.
   - Integrate with monitoring tools (e.g., Prometheus) if required.

7. **Testing & CI/CD Validation**
   - Execute all test cases from `test-cases.md`.
   - Validate remote access, security, resilience, and auditability.
   - Verify CI/CD integration:
     - Test automated deployment with tunnel service restart.
     - Verify tunnel configuration updates through version control.
     - Validate deployment verification includes tunnel connectivity checks.
     - Ensure self-hosted runner has proper access to manage tunnel service.

8. **Documentation**
    - Follow the Divio documentation strategy:
       - Place troubleshooting guides in `/docs/guides/` as `cloudflare-tunnel-troubleshooting.md`.
       - Link to the troubleshooting guide from this implementation plan and from the main requirements documentation.
       - Use clear section headings: `# Troubleshooting Cloudflare Tunnel`, `## Common Issues`, `## Diagnostic Steps`, `## Solutions`, and `## References`.
       - Update `/docs/requirements/cloudflare-tunnel/specifications.md` and `/docs/requirements/cloudflare-tunnel/implementation.md` to reference the troubleshooting guide in the "See also" section.
       - Ensure all documentation is in Markdown and follows the Divio/README structure for clarity and traceability.

---

See also: [Specifications](specifications.md), [Test Cases](test-cases.md), [Diagrams](diagrams/).
