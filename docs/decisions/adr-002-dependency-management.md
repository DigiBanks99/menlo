# ADR-002: Dependency Management Strategy

## Status

**Accepted** - 2024-11-24

## Context

The Menlo home management application requires an automated dependency management solution to keep both .NET and frontend (Angular/npm) dependencies up to date while ensuring security and stability.
The project initially implemented a custom GitHub Actions workflow using `dotnet-outdated` and `pnpm outdated` tools, but encountered several significant challenges:

### Issues with Custom GitHub Actions Approach

- **GitHub Actions Output Limitations**: Complex JSON output from dependency tools caused parsing errors when storing in `$GITHUB_OUTPUT` variables
- **High Maintenance Overhead**: Custom parsing logic, error handling, and output formatting required ongoing maintenance
- **Reliability Issues**: Tool version compatibility issues and brittle string parsing logic
- **Complex Debugging**: Difficult to troubleshoot failures when custom scripts failed
- **Limited Native Integration**: Poor integration with GitHub's native dependency management features

### Requirements

- Support for .NET (NuGet) and npm/pnpm ecosystems
- Automated security vulnerability updates
- Weekly scheduled updates with exclusion of major version changes for stability
- Grouped pull requests to reduce noise
- Low maintenance overhead
- High reliability and native GitHub integration

## Options Considered

### 1. Continue with Custom GitHub Actions

- **Pros**: Maximum customization, full control over update logic
- **Cons**: High maintenance overhead, reliability issues, GitHub Actions limitations, complex debugging
- **Assessment**: Not sustainable for long-term maintenance

### 2. Third-party Tools (Renovate)

- **Pros**: Highly configurable, excellent monorepo support, auto-merge capabilities
- **Cons**: Additional third-party dependency, more complex configuration
- **Assessment**: Good for complex scenarios, but unnecessary complexity for this project

### 3. GitHub Dependabot (Recommended)

- **Pros**: Native GitHub integration, zero maintenance, built-in security updates, free, well-documented
- **Cons**: Less granular control than custom solutions, limited scheduling options
- **Assessment**: Best balance of functionality and simplicity

## Decision

**Selected**: GitHub Dependabot

We will replace the custom GitHub Actions dependency management workflow with GitHub's native Dependabot service.

### Rationale

1. **Zero Maintenance**: Dependabot requires no custom code or ongoing maintenance
2. **Native Integration**: Built-in GitHub features with no GitHub Actions output limitations
3. **Security Focus**: Automatic security vulnerability updates
4. **Reliability**: Proven, stable service used by millions of repositories
5. **Cost Effective**: Free for all GitHub plans
6. **Ecosystem Support**: Native support for both .NET (NuGet) and npm ecosystems
7. **Smart Grouping**: Built-in PR grouping to reduce notification noise

### Configuration Strategy

- **Update Schedule**: Weekly updates on Sundays at 02:00 UTC to match current schedule
- **Version Policy**: Exclude major version updates to maintain stability
- **Grouping**: Group minor and patch updates by ecosystem to reduce PR volume
- **Security Updates**: Enable automatic security updates for immediate vulnerability patching
- **PR Limits**: Allow up to 10 open PRs per ecosystem to prevent overwhelming maintainers

## Implementation Plan

1. **Create Dependabot Configuration**: Add `.github/dependabot.yml` with ecosystem-specific settings
2. **Enable Dependabot**: Activate Dependabot version updates in repository settings
3. **Remove Custom Workflow**: Delete `.github/workflows/dependency-updates.yml`
4. **Clean Up Tools**: Remove `dotnet-outdated-tool` from `.config/dotnet-tools.json` if no longer needed
5. **Optional Auto-merge**: Consider adding simple auto-merge workflow for patch updates

## Consequences

### Positive

- **Reduced Complexity**: Elimination of 400+ lines of complex workflow code
- **Improved Reliability**: No more GitHub Actions output parsing issues
- **Better Security**: Automatic security updates with vulnerability detection
- **Lower Maintenance**: Zero ongoing maintenance requirements
- **Better Integration**: Native GitHub PR management and notifications

### Negative

- **Less Control**: Cannot customize update logic beyond Dependabot's configuration options
- **Limited Scheduling**: Weekly/monthly intervals only, no complex scheduling
- **Dependency on GitHub**: Tied to GitHub's service availability and feature set

### Risks and Mitigations

- **Risk**: Dependabot may miss some edge cases handled by custom logic
- **Mitigation**: Monitor initial deployments and supplement with manual checks if needed

- **Risk**: Loss of detailed reporting provided by custom workflow
- **Mitigation**: Dependabot PRs include detailed change information and security context

## Related Decisions

- ADR-001: Hosting Strategy - This decision aligns with the cost-conscious approach by eliminating custom maintenance overhead

## References

- [GitHub Dependabot Documentation](https://docs.github.com/en/code-security/dependabot)
- [Dependabot Configuration Reference](https://docs.github.com/en/code-security/dependabot/working-with-dependabot/dependabot-options-reference)
- [GitHub Actions vs Native Tools Discussion](https://github.com/orgs/community/discussions)
