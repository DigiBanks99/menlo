# Repo Structure (Scaffold)

```mermaid
flowchart TD
  A[repo root] --> B[src/]
  A --> C[docs/]
  A --> D[.github/]
  B --> B1[api/]
  B --> B2[lib/]
  B --> B3[ui/]
  B1 --> B11[Menlo.Api/]
  B1 --> B12[Menlo.AppHost/]
  B1 --> B13[Menlo.Api.Tests/]
  B2 --> B21[Menlo.Lib/]
  B2 --> B22[Menlo.AI/]
  B2 --> B23[Menlo.ServiceDefaults/]
  B3 --> B31[web/]
```

Notes:

- Target framework: .NET 9 for all projects per architecture document.
- Central package management via Directory.Packages.props.
- API wired with ServiceDefaults and default health endpoints (development).
- Aspire AppHost references Menlo.Api as a distributed resource.
- Initial API smoke test verifies startup and basic endpoint.
