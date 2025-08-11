# Menlo Repository Scaffolding & Code Structure — Implementation Plan (Clean Slate)

## 1. Preparation

- Review [architecture docs](../../explanations/architecture-document.md), [C4 diagrams](../../diagrams/c4-code-diagram.md), and [Angular instructions](../../../.github/instructions/angular.instructions.md).
- Confirm requirements in [specifications.md](./specifications.md) and [test-cases.md](./test-cases.md).

## 2. Top-Level Folders

- At repo root, create:

```sh
mkdir src docs .github
```

## 3. Solution & Templates

- Install Aspire templates (if not already):

```sh
dotnet new install Aspire.ProjectTemplates
```

- Create solution:

```sh
cd src
dotnet new sln --name Menlo --format slnx
```

## 4. Backend (Strict Sequential Order)

### 4.1. AppHost

```sh
dotnet new aspire-apphost -n Menlo.AppHost -o api/Menlo.AppHost
dotnet sln add api/Menlo.AppHost/Menlo.AppHost.csproj
```

### 4.2. ServiceDefaults

```sh
dotnet new aspire-servicedefaults -n Menlo.ServiceDefaults -o lib/Menlo.ServiceDefaults
dotnet sln add lib/Menlo.ServiceDefaults/Menlo.ServiceDefaults.csproj
```

### 4.3. Shared Libraries

```sh
dotnet new classlib -n Menlo.Lib -o lib/Menlo.Lib
dotnet new classlib -n Menlo.AI -o lib/Menlo.AI
dotnet sln add lib/Menlo.Lib/Menlo.Lib.csproj
dotnet sln add lib/Menlo.AI/Menlo.AI.csproj
```

### 4.4. Minimal API

```sh
dotnet new webapi -n Menlo.Api -o api/Menlo.Api --use-minimal-apis
dotnet sln add api/Menlo.Api/Menlo.Api.csproj
```

### 4.5. Add References (in order)

```sh
# Menlo.Lib and Menlo.AI to Menlo.Api
dotnet add api/Menlo.Api/Menlo.Api.csproj reference ../lib/Menlo.Lib/Menlo.Lib.csproj
dotnet add api/Menlo.Api/Menlo.Api.csproj reference ../lib/Menlo.AI/Menlo.AI.csproj
# ServiceDefaults to Menlo.Api
dotnet add api/Menlo.Api/Menlo.Api.csproj reference ../lib/Menlo.ServiceDefaults/Menlo.ServiceDefaults.csproj
# Menlo.Api and ServiceDefaults to AppHost
dotnet add api/Menlo.AppHost/Menlo.AppHost.csproj reference ../Menlo.Api/Menlo.Api.csproj
dotnet add api/Menlo.AppHost/Menlo.AppHost.csproj reference ../lib/Menlo.ServiceDefaults/Menlo.ServiceDefaults.csproj
```

### 4.6. Configure Service Defaults

- In `Menlo.Api/Program.cs`:

  ```csharp
  var builder = WebApplication.CreateBuilder(args);
  builder.AddServiceDefaults();
  // ...
  var app = builder.Build();
  app.MapDefaultEndpoints();
  app.Run();
  ```
- In `Menlo.AppHost/Program.cs`:

  ```csharp
  var builder = DistributedApplication.CreateBuilder(args);
  builder.AddServiceDefaults();
  var api = builder.AddProject<Projects.Menlo_Api>("menlo-api");
  builder.Build().Run();
  ```

### 4.7. (Optional) EF Core Migrations

```sh
dotnet tool install --global dotnet-ef
dotnet add lib/Menlo.Lib/Menlo.Lib.csproj package Microsoft.EntityFrameworkCore
dotnet add api/Menlo.Api/Menlo.Api.csproj package Microsoft.EntityFrameworkCore.Design
```

## 5. Frontend (Angular)

### 5.1. Use pnpm for Package Management

- Install pnpm globally:

  ```sh
  npm install -g pnpm
  ```
- Use `pnpm install` for all dependency management and `pnpm link` for local package linking between workspace libraries and apps.

### 5.2. Scaffold Angular Workspace and Libraries

- Install Angular CLI:

  ```sh
  pnpm add -g @angular/cli
  ```
- Scaffold a multi-project Angular workspace under `src/ui/web`:

  ```sh
  cd src/ui
  pnpm ng new web --create-application false --strict --style scss
  cd web
  # Application (shell)
  pnpm ng g application menlo-app --standalone --routing --style scss
  # Libraries (separation by responsibility)
  pnpm ng g library shared-ui --standalone --style scss
  pnpm ng g library shared-util
  pnpm ng g library data-access-menlo-api
  ```

### 5.3. Vite as Dev Server (Recommended)

- Angular CLI uses Vite as the default dev server (Angular 17+). For custom Vite builds or advanced features, use [AnalogJS Vite plugin for Angular](https://analogjs.org/docs/packages/vite-plugin-angular/overview).
- To run the dev server:

  ```sh
  pnpm start # or pnpm run start
  # For custom Vite: pnpm vite
  ```

### 5.4. Vitest for Unit Testing

- Angular supports Vitest as an experimental test runner. Update the `test` target in `angular.json`:

  ```json
  "test": {
    "builder": "@angular/build:unit-test",
    "options": {
      "tsConfig": "tsconfig.spec.json",
      "runner": "vitest",
      "buildTarget": "::development"
    }
  }
  ```
- Run tests with:

  ```sh
  pnpm test
  # or
  pnpm ng test
  ```

### 5.5. Storybook for UI Libraries

- Add Storybook with Vite builder:

  ```sh
  pnpm dlx storybook@latest init --builder @storybook/builder-vite
  ```
- Place stories in your UI libraries (e.g., `shared-ui`).
- Run Storybook:

  ```sh
  pnpm storybook
  ```

### 5.6. Dev Proxy and HTTPS for Local Dev

- Generate a local HTTPS certificate (e.g., with [mkcert](https://github.com/FiloSottile/mkcert)) and configure Vite/Angular CLI to use it.
- Create `proxy.conf.ts` in `src/ui/web`:

  ```ts
  import { defineProxyConfig } from '@angular-devkit/build-angular';

  export default defineProxyConfig({
    '/api': {
      target: 'https://localhost:PORT', // Menlo.Api HTTPS port
      secure: false,
      changeOrigin: true,
      logLevel: 'debug',
    },
  });
  ```
- Update `angular.json` serve options for `menlo-app` to use the proxy:

  ```json
  {
    "projects": {
      "menlo-app": {
        "architect": {
          "serve": {
            "options": {
              "proxyConfig": "proxy.conf.ts",
              "ssl": true,
              "sslKey": "path/to/localhost-key.pem",
              "sslCert": "path/to/localhost.pem"
            }
          }
        }
      }
    }
  }
  ```
- Ensure Aspire and Menlo.Api are also configured for HTTPS with the same certificate if possible.

### 5.7. App Bootstrap Example

- In `menlo-app` bootstrap, enable modern providers and signals-friendly HttpClient:

  ```ts
  import { bootstrapApplication } from '@angular/platform-browser';
  import { AppComponent } from './app/app.component';
  import { provideRouter } from '@angular/router';
  import { provideHttpClient, withFetch } from '@angular/common/http';

  bootstrapApplication(AppComponent, {
    providers: [
      provideRouter([]),
      provideHttpClient(withFetch()),
    ],
  });
  ```

### 5.8. Recommended Structure

- App: `menlo-app` (shell, routing, layout)
- Feature libs: `features/<domain>` as buildable libraries when needed
- Shared libs: `shared-ui`, `shared-util`, `data-access-menlo-api` (API client + facades)
- Use standalone components, signals, OnPush, native control flow

### 5.9. Optional Enhancements

- SSR/SSG for SEO and performance:

  ```sh
  pnpm ng add @angular/ssr --project menlo-app
  ```

## 6. AI Infrastructure (Ollama & Semantic Kernel)

- Add Ollama integration:
  - In AppHost:
    
    ```sh
    dotnet add api/Menlo.AppHost/Menlo.AppHost.csproj package CommunityToolkit.Aspire.Hosting.Ollama
    ```
  - In Menlo.Api:
    
    ```sh
    dotnet add api/Menlo.Api/Menlo.Api.csproj package CommunityToolkit.Aspire.OllamaSharp
    ```
- Add Semantic Kernel:
  
  ```sh
  dotnet add lib/Menlo.AI/Menlo.AI.csproj package Microsoft.SemanticKernel
  ```
  
  - Note: Add a direct `Microsoft.SemanticKernel` reference to `Menlo.Api` only if it uses SK types directly; otherwise it flows transitively via `Menlo.AI`.
- Register and configure in code as per official docs.

## 7. Documentation & Validation

- Store all docs and diagrams in `docs/`.
- Update C4 diagrams as structure evolves.
- Validate structure against [test-cases.md](./test-cases.md) after each major step.
- Run CI/CD pipelines to ensure build and test success.

---

> ℹ️ **Tip:** After every step, the solution should compile and run without warnings. Never reference a project or file before it exists. Always update documentation and diagrams as the repo evolves.

## Gotchas

> - ℹ️ **Gotcha**: Avoid mixing domain logic and infrastructure in the same folder. Keep boundaries clear to support maintainability and testability.
> - ℹ️ **Gotcha**: Always update documentation and diagrams when changing the structure.
