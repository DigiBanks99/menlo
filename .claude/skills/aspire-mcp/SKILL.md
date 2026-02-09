---
name: aspire-mcp
description: Use Aspire to monitor, debug, and manage distributed application resources including services, databases, containers, logs, traces, and integrations
---

# Aspire MCP Server Integration


## Available MCP Tools

### Resource Management

**`list_resources`** - Lists all resources with state, health, endpoints, and available commands.

**`execute_resource_command`** - Executes commands (Restart, Stop, Start) on specific resources.
- Parameters: `resource_name`, `command_name`

### Logging and Debugging

**`list_console_logs`** - Retrieves console output logs for debugging startup errors and crashes.
- Parameters: `resource_name`

**`list_structured_logs`** - Gathers structured logs with optional filtering by resource.
- Parameters: `resource_name` (optional)

**`list_traces`** - Fetches distributed traces for performance analysis.
- Parameters: `resource_name` (optional)

**`list_trace_structured_logs`** - Extracts logs associated with a specific trace for correlation.
- Parameters: `trace_id`

### Integration Discovery

**`list_integrations`** - Lists available Aspire hosting integrations with package IDs and versions.

**`get_integration_docs`** - Retrieves documentation for specific integration packages.
- Parameters: `package_id`

### AppHost Management

**`list_apphosts`** - Shows all available AppHosts in the workspace.

**`select_apphost`** - Sets the active AppHost for operations.
- Parameters: `apphost_name`

## Common Workflows

### Debugging Failures
1. List resources to check status
2. Check console/structured logs for errors
3. Analyze traces
4. Fix code and restart affected resources

### Performance Analysis
1. Get traces
2. Examine trace logs
3. Check resource logs
4. Optimize and verify improvement

### Adding Integrations
1. Search available integrations
2. Get integration documentation
3. Add to AppHost following docs
4. Verify resource appears and is healthy

## Resource Exclusion

Exclude sensitive resources from MCP access using `.ExcludeFromMcp()`:

```csharp
var secretsDb = builder.AddPostgres("secrets-db")
    .ExcludeFromMcp(); // Not visible to AI agents
```

## Troubleshooting

| Issue                     | Solution                                                                              |
| ------------------------- | ------------------------------------------------------------------------------------- |
| MCP server not responding | Verify Aspire CLI installed, AppHost running, `.mcp.json` exists, restart Claude Code |
| No resources listed       | Check AppHost is running and selected correctly                                       |
| Cannot execute commands   | Verify resource name (case-sensitive) and command availability                        |
| Logs not available        | Resources must be running; check structured logs if console logs missing              |

## Limitations

- Requires Aspire 13+
- Resources must be running for logs/traces
- Trace retention: 15-30 minutes
- Local only (no remote AppHost access)

## References

- [Aspire MCP Server Documentation](https://aspire.dev/dashboard/mcp-server/)
- [Configure the MCP Server](https://aspire.dev/get-started/configure-mcp/)
- [Model Context Protocol Specification](https://modelcontextprotocol.io)
