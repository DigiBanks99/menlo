# SPA build stage
FROM node:22-slim AS spa-build

ENV PNPM_HOME=/pnpm
ENV PATH=$PNPM_HOME:$PATH

WORKDIR /app

RUN corepack enable

COPY package.json ./
COPY pnpm-lock.yaml ./
COPY pnpm-workspace.yaml ./
COPY src/ui/web/ ./src/ui/web/

RUN pnpm install --frozen-lockfile
RUN pnpm --dir src/ui/web build:prod

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Set working directory
WORKDIR /app

# Copy project files
COPY Menlo.slnx ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

# Copy source code
COPY src/ ./src/

# Restore dependencies
RUN dotnet restore

# Build the application
RUN dotnet build --no-restore --configuration Release

# Test stage (optional - can be skipped in production builds)
FROM build AS test
RUN dotnet test --no-build --configuration Release --verbosity normal

# Publish stage
FROM build AS publish
RUN dotnet publish src/api/Menlo.Api/Menlo.Api.csproj \
    --no-build \
    --configuration Release \
    --output /app/publish \
    --self-contained false \
    --verbosity normal

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Create app user (use shadow utils commands)
RUN groupadd --system --gid 1001 appgroup && \
    useradd --system --uid 1001 --gid 1001 --create-home appuser

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .
COPY --from=spa-build /app/src/ui/web/dist/menlo-app/browser/ ./wwwroot/

# Change ownership to app user
RUN chown -R appuser:appgroup /app

# Switch to app user
USER appuser

# Expose port
EXPOSE 8080

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "Menlo.Api.dll"]
