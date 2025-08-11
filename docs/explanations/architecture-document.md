# Menlo Home Management - Architecture Document

## Table of Contents

- [Menlo Home Management - Architecture Document](#menlo-home-management---architecture-document)
  - [Table of Contents](#table-of-contents)
  - [Executive Summary](#executive-summary)
  - [Domain-Driven Architecture \& Vertical Slice Organization](#domain-driven-architecture--vertical-slice-organization)
    - [Architectural Foundation](#architectural-foundation)
    - [Domain Boundaries \& Bounded Contexts](#domain-boundaries--bounded-contexts)
    - [Architectural Constraints](#architectural-constraints)
  - [Technology Stack \& Platform Strategy](#technology-stack--platform-strategy)
    - [Core Technology Standards](#core-technology-standards)
    - [Hosting Strategy](#hosting-strategy)
    - [Technology Constraints](#technology-constraints)
  - [Data Architecture \& Privacy Framework](#data-architecture--privacy-framework)
    - [Data Storage Strategy](#data-storage-strategy)
    - [Data Privacy \& Security Principles](#data-privacy--security-principles)
    - [Data Architecture Constraints](#data-architecture-constraints)
  - [AI Integration Architecture](#ai-integration-architecture)
    - [Local AI Strategy](#local-ai-strategy)
    - [AI Architecture Pattern](#ai-architecture-pattern)
    - [AI Service Categories](#ai-service-categories)
    - [AI Architecture Constraints](#ai-architecture-constraints)
  - [Scale \& Performance Framework](#scale--performance-framework)
    - [Target Scale](#target-scale)
    - [Performance Requirements](#performance-requirements)
    - [Resource Constraints](#resource-constraints)
    - [Performance Architecture Constraints](#performance-architecture-constraints)
  - [Security \& Authentication Architecture](#security--authentication-architecture)
    - [Authentication Strategy](#authentication-strategy)
    - [Authorization Model](#authorization-model)
    - [Security Constraints](#security-constraints)
  - [Development Workflow \& CI/CD Architecture](#development-workflow--cicd-architecture)
    - [Development Process](#development-process)
    - [CI/CD Pipeline Architecture](#cicd-pipeline-architecture)
    - [Development Environment Standards](#development-environment-standards)
    - [Development Constraints](#development-constraints)
  - [Code Organization \& Design Patterns](#code-organization--design-patterns)
    - [Architectural Patterns](#architectural-patterns)
    - [Code Organization Structure](#code-organization-structure)
    - [Design Pattern Standards](#design-pattern-standards)
    - [Code Quality Constraints](#code-quality-constraints)
  - [Error Handling \& Resilience Framework](#error-handling--resilience-framework)
    - [Error Handling Strategy](#error-handling-strategy)
    - [Error Response Architecture](#error-response-architecture)
    - [Resilience Patterns](#resilience-patterns)
    - [Error Handling Constraints](#error-handling-constraints)
  - [Monitoring, Observability \& Operations](#monitoring-observability--operations)
    - [Observability Strategy](#observability-strategy)
    - [Health Monitoring Framework](#health-monitoring-framework)
    - [Operational Procedures](#operational-procedures)
    - [Operational Constraints](#operational-constraints)
  - [Architecture Implementation Roadmap](#architecture-implementation-roadmap)
    - [Phase 1: Foundation modelling](#phase-1-foundation-modelling)
    - [Phase 2: Core Features modelling](#phase-2-core-features-modelling)
    - [Phase 3: AI Enhancement modelling](#phase-3-ai-enhancement-modelling)
    - [Phase 4: Polish \& Family Validation modelling](#phase-4-polish--family-validation-modelling)
  - [Architectural Decision Records (ADRs)](#architectural-decision-records-adrs)
  - [Conclusion](#conclusion)

## Executive Summary

This document defines the comprehensive architecture for the Menlo Home Management application, establishing the foundational principles, patterns, and constraints that will guide all future development. The architecture embodies a **cost-conscious, family-first, AI-enhanced** approach to home management software, designed to respect natural workflows while providing intelligent automation.

**Core Philosophy**: "*The Blueberry Muffin Approach*" - AI enhances existing processes without replacing them, like blueberries in a muffin.

---

## Domain-Driven Architecture & Vertical Slice Organization

### Architectural Foundation

**Pattern**: **Rich Domain Model with Vertical Slice Architecture**

The system is organized as independent vertical slices, each representing a complete feature from UI to database:

```tree
Planning & Inventory Slice
├── UI Components (Angular)
├── API Endpoints (Minimal API)
├── Command/Query Handlers
├── Domain Models & Business Logic
├── AI Domain Services
└── Data Repository

Budget Management Slice
Financial Management Slice
Event Management Slice
Household Management Slice
```

### Domain Boundaries & Bounded Contexts

The system is organized into **5 primary bounded contexts**:

| Bounded Context | Aggregate Root | Primary Responsibility | AI Integration |
|---|---|---|---|
| **Planning & Inventory** | `PlanningList` | Handwritten list capture, meal planning, inventory | `ListPhotoInterpreter`, `BudgetImpactAnalyser` |
| **Budget Management** | `Budget` | Hierarchical budget categories, allocation tracking | `BudgetAnalyser`, `TransactionCategorizer` |
| **Financial Management** | `FinancialAccount` | Bank transaction import, categorization, reconciliation | `ImportProcessor`, `AttributionSuggester` |
| **Event Management** | `Event` | Calendar scheduling, time planning | `SmartScheduler`, `AutomatedEventCreator` |
| **Household Management** | `Household`/`UtilityAccount` | Family structure, appliances, SA utilities | `ApplianceMaintenanceRequirements`, `UtilityOptimizer` |

### Architectural Constraints

**CONSTRAINT AC-001**: Each vertical slice must be independently deployable and testable
**CONSTRAINT AC-002**: Cross-slice communication occurs only through integration events
**CONSTRAINT AC-003**: AI services are injected as domain interfaces, never concrete implementations
**CONSTRAINT AC-004**: Domain models contain business logic; controllers are thin orchestration layers

---

## Technology Stack & Platform Strategy

### Core Technology Standards

**Backend Stack** (Fixed Architecture Decisions):

- **Runtime**: .NET 9.0 with C# 12
- **Web Framework**: ASP.NET Core Minimal APIs
- **Domain Logic**: Rich domain models with CSharpFunctionalExtensions
- **AI Orchestration**: Microsoft Semantic Kernel
- **Local AI**: Ollama with Phi-4-mini/Phi-4-vision models

**Frontend Stack** (Fixed Architecture Decisions):

- **Framework**: Angular 20+ with TypeScript
- **UI Library**: Bootstrap 5 with Angular Material components
- **State Management**: Angular Signals (RxJS for complex flows)
- **Build**: Angular CLI with Vite

**Infrastructure Standards**:

- **Data Storage**: PostgreSQL with Entity Framework Core
- **AI Infrastructure**: Local Ollama server (no external AI APIs)
- **Authentication**: Azure AD with Microsoft Identity Platform
- **Deployment**: Docker containers via GitHub Actions

### Hosting Strategy

**ARCHITECTURAL DECISION**: **Hybrid Cloud-Local Architecture** (per [ADR-001](../decisions/adr-001-hosting-strategy.md))

```md
[Azure Static Web Apps] → [Cloudflare Tunnel] → [Home Server]
     (Frontend)              (Secure Proxy)      (Backend + AI)
```

**Benefits**:

- **Cost Control**: ~R165-365/month total hosting cost
- **Privacy First**: All data and AI processing remains local
- **Global Performance**: CDN-distributed frontend
- **Security**: No exposed IP addresses or ports

### Technology Constraints

**CONSTRAINT TC-001**: No external AI service dependencies (OpenAI, Azure AI, etc.)
**CONSTRAINT TC-002**: All AI processing must run locally via Ollama
**CONSTRAINT TC-003**: Data storage must remain local (no cloud databases)
**CONSTRAINT TC-004**: Authentication via Azure AD only (no social logins)

---

## Data Architecture & Privacy Framework

### Data Storage Strategy

**Primary Data Store**: **PostgreSQL** (migration from current Cosmos DB)

**Data Organization**:

```tree
menlo_db
├── planning_schema (Planning & Inventory context)
├── budget_schema (Budget Management context)  
├── financial_schema (Financial Management context)
├── events_schema (Event Management context)
├── household_schema (Household Management context)
└── shared_schema (Cross-cutting concerns)
```

### Data Privacy & Security Principles

**PRINCIPLE DP-001**: **No PII leaves the home server**

- All personal financial data remains local
- Family information stored locally only
- Bank transaction details never transmitted to external services

**PRINCIPLE DP-002**: **Encryption at rest and in transit**

- PostgreSQL configured with transparent data encryption
- HTTPS/TLS for all API communications
- Cloudflare tunnel provides additional encryption layer

**PRINCIPLE DP-003**: **Data sovereignty and portability**

- Family owns all data completely
- Export capabilities for all data in standard formats
- No vendor lock-in for core family data

### Data Architecture Constraints

**CONSTRAINT DA-001**: Personal financial data never stored in cloud services
**CONSTRAINT DA-002**: All database connections use connection pooling and prepared statements
**CONSTRAINT DA-003**: Soft deletes for all user-generated content (auditing requirements)
**CONSTRAINT DA-004**: Database schema changes via Entity Framework migrations only

---

## AI Integration Architecture

### Local AI Strategy

**Core Principle**: **"Blueberry Muffin" AI Integration**

AI enhances existing workflows without replacing human decision-making:

```md
Human Workflow + AI Enhancement = Better Outcome
(Not: AI Replacement → Workflow)
```

### AI Architecture Pattern

**Pattern**: **Agentic AI as Domain Services**

```csharp
// Domain Layer - AI services injected as interfaces
public class PlanningList : AggregateRoot
{
    private readonly IBudgetImpactAnalyser _budgetAnalyser;
    private readonly IListPhotoInterpreter _listInterpreter;
    
    public async Task<Result<BudgetImpact>> AnalyzeBudgetImpact()
    {
        return await _budgetAnalyser.AnalyzeAsync(this);
    }
}

// Infrastructure Layer - Concrete AI implementations
public class SemanticKernelBudgetAnalyser : IBudgetImpactAnalyser
{
    private readonly Kernel _kernel;
    private readonly IOllamaService _ollama;
    
    // Implementation uses Semantic Kernel + local Ollama
}
```

### AI Service Categories

| AI Service Category | Purpose | Model Requirements | Learning Strategy |
|---|---|---|---|
| **Text Processing** | Handwritten list interpretation | Phi-4-vision | User correction feedback |
| **Financial Analysis** | Transaction categorization, attribution | Phi-4-mini | Historical pattern learning |
| **Planning Optimization** | Schedule conflict detection, meal planning | Phi-4-mini | Family preference learning |
| **Predictive Insights** | Budget variance, maintenance scheduling | Phi-4-mini | Time-series pattern recognition |

### AI Architecture Constraints

**CONSTRAINT AI-001**: All AI models must run locally via Ollama (no external API calls)
**CONSTRAINT AI-002**: AI services implement domain interfaces, never directly coupled to models
**CONSTRAINT AI-003**: User corrections trigger immediate model fine-tuning where applicable
**CONSTRAINT AI-004**: AI failures must degrade gracefully (system remains functional)
**CONSTRAINT AI-005**: AI suggestions are always reviewable and reversible by users

---

## Scale & Performance Framework

### Target Scale

**User Scale**: **Single family (2 adults, 2+ children) maximum**

The application is explicitly designed for family-scale usage:

| Metric | Target | Maximum | Design Constraint |
|---|---|---|---|
| **Concurrent Users** | 2-4 | 6 | Family members only |
| **Monthly Transactions** | 200-500 | 1,000 | Typical family banking |
| **Planning Lists/Month** | 10-20 | 50 | Weekly shopping + events |
| **Budget Categories** | 50-100 | 200 | Detailed personal budgeting |
| **Data Growth** | 10MB/month | 100MB/month | Text and small images only |

### Performance Requirements

**Response Time Targets**:

- **Interactive Operations**: < 200ms (button clicks, form submissions)
- **AI Processing**: < 2 seconds (text interpretation, categorization)
- **Complex AI Analysis**: < 10 seconds (budget analysis, schedule optimization)
- **Data Import**: < 30 seconds (bank CSV processing)

### Resource Constraints

**Home Server Specifications** (minimum):

- **CPU**: 4 cores, 2.5GHz (for Ollama model inference)
- **Memory**: 16GB RAM (8GB for Ollama, 4GB for application, 4GB OS)
- **Storage**: 100GB SSD (models, database, application)
- **Network**: Stable broadband (for Cloudflare tunnel)

### Performance Architecture Constraints

**CONSTRAINT PC-001**: Database queries must use indexes and limit result sets
**CONSTRAINT PC-002**: AI model inference cannot block user interface operations
**CONSTRAINT PC-003**: Background processing required for compute-intensive AI operations
**CONSTRAINT PC-004**: Caching strategies for frequently accessed data (budget categories, family config)

---

## Security & Authentication Architecture

### Authentication Strategy

**Primary Authentication**: **Azure AD with Microsoft Identity Platform**

```md
User → [Azure AD Login] → [JWT Token] → [Home Server Validation] → [Authorized Access]
```

**Authentication Flow**:

1. User accesses application via Azure Static Web Apps
2. Azure AD authentication redirect for unauthenticated users
3. JWT token issued by Azure AD upon successful authentication
4. Home server validates JWT token signature and claims
5. Policy-based authorization applied to API endpoints

### Authorization Model

**Pattern**: **Policy-Based Access Control**

```csharp
// Role-based policies for different access levels
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FamilyMember", policy => 
        policy.RequireRole("FamilyMember"));
    
    options.AddPolicy("FinancialAccess", policy => 
        policy.RequireRole("FinancialAccess"));
    
    options.AddPolicy("AdminAccess", policy => 
        policy.RequireRole("AdminAccess"));
});
```

**Authorization Hierarchy**:

- **AdminAccess**: Full system access (primary family member)
- **FinancialAccess**: Budget and financial data access
- **FamilyMember**: Planning lists, events, basic household features
- **ReadOnly**: View-only access for shared information

### Security Constraints

**CONSTRAINT SC-001**: All API endpoints require authentication (no anonymous access)
**CONSTRAINT SC-002**: Sensitive operations require elevated roles (financial data, system configuration)
**CONSTRAINT SC-003**: HTTPS required for all communications (no HTTP fallback)
**CONSTRAINT SC-004**: JWT tokens must be validated and refreshed according to Azure AD policies
**CONSTRAINT SC-005**: Development environment can bypass authentication for testing only

---

## Development Workflow & CI/CD Architecture

### Development Process

**Branching Strategy**: **GitHub Flow with Feature Branches**

```tree
main (production-ready)
├── feature/planning-list-ai-enhancement
├── feature/budget-category-redesign
└── hotfix/authentication-bug-fix
```

**Development Workflow**:

1. **Feature Development**: Create feature branch from `main`
2. **Local Development**: Use .NET Aspire for local orchestration
3. **Testing**: Unit tests, integration tests, and family acceptance testing
4. **Pull Request**: Code review and automated testing
5. **Merge to Main**: Triggers deployment pipeline
6. **Production Deployment**: Automated deployment to home server

### CI/CD Pipeline Architecture

**Platform**: **GitHub Actions** with Azure integration

**Pipeline Stages**:

```md
[Code Push] → [Build & Test] → [Container Build] → [Deploy to Production]
     ↓              ↓               ↓                    ↓
  Trigger        Unit Tests     Docker Image         Home Server
  Workflow      Integration     Push to Registry     Container Update
                E2E Tests       Security Scan       Health Checks
```

**Deployment Strategy**: **Blue-Green with Cloudflare Tunnel**

- Zero-downtime deployments via container orchestration
- Cloudflare tunnel automatically routes to healthy container
- Rollback capability within 5 minutes

### Development Environment Standards

**Local Development Stack**:

- **.NET Aspire**: Service orchestration and dependency management
- **Docker Compose**: Database and supporting services
- **Ollama**: Local AI model development and testing
- **Angular CLI**: Frontend development server with hot reload

### Development Constraints

**CONSTRAINT DC-001**: All code must pass automated tests before merge to main
**CONSTRAINT DC-002**: Feature branches cannot exceed 5 days without merge or rebase
**CONSTRAINT DC-003**: Production deployments only from main branch
**CONSTRAINT DC-004**: Database schema changes require migration scripts and rollback plans

---

## Code Organization & Design Patterns

### Architectural Patterns

**Primary Pattern**: **Rich Domain Model with Agentic AI**

**Core Principles**:

- **Domain-Centric Design**: Business logic lives in domain models, not controllers
- **Inversion of Control**: Dependencies injected via interfaces
- **Single Responsibility**: Each class/service has one clear purpose
- **Dependency Inversion**: High-level modules don't depend on low-level modules

### Code Organization Structure

```tree
src/
├── api/                         # Backend API layer
│   ├── Menlo.Api/               # Main API host
│   │   ├── Planning/            # Planning slice endpoints
│   │   ├── Budget/              # Budget slice endpoints
│   │   ├── Financial/           # Financial slice endpoints
│   │   ├── Events/              # Events slice endpoints
│   │   └── Household/           # Household slice endpoints
│   └── Menlo.AppHost/           # Aspire AppHost
├── lib/                         # Domain and infrastructure
│   ├── Menlo.Lib/               # Core domain library
│   │   ├── Planning/            # Planning domain
│   │   ├── Budget/              # Budget domain
│   │   ├── Financial/           # Financial domain
│   │   ├── Events/              # Events domain
│   │   ├── Household/           # Household domain
│   │   └── Common/              # Shared domain patterns
│   ├── Menlo.AI/                # AI infrastructure layer
│   └── Menlo.ServiceDefaults/   # Shared infrastructure
└── ui/                          # Frontend application
    └── web/                     # Angular application
        ├── projects/menlo-app/  # Main application
        └── projects/menlo-lib/  # Shared UI components
```

### Design Pattern Standards

**Error Handling**: **Result Pattern with CSharpFunctionalExtensions**

```csharp
public interface ICommandHandler<TRequest, TResponse>
{
    Task<Response<TResponse, MenloError>> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

// Usage promotes functional error handling
var result = await handler.HandleAsync(command, cancellationToken);
return result.IsSuccess 
    ? Results.Ok(result.Data)
    : Results.Problem(result.Error.Message);
```

**Event Communication**: **Integration Events for Cross-Aggregate Communication**

```csharp
public record PlanningListRealized(
    PlanningListId ListId,
    IReadOnlyList<PlanningItem> Items,
    DateTime RealizedAt,
    PersonId RealizedBy
) : IIntegrationEvent;
```

### Code Quality Constraints

**CONSTRAINT CQ-001**: All public APIs must return Result<T> or Response<T, TError> types
**CONSTRAINT CQ-002**: Domain models cannot reference infrastructure concerns
**CONSTRAINT CQ-003**: Controllers must be thin (< 20 lines per action)
**CONSTRAINT CQ-004**: All AI services must implement domain interfaces
**CONSTRAINT CQ-005**: Integration events required for cross-aggregate communication

---

## Error Handling & Resilience Framework

### Error Handling Strategy

**Core Pattern**: **Result Pattern with Domain-Friendly Errors**

**Error Categories**:

1. **Domain Errors**: Business rule violations (user-correctable)
2. **Infrastructure Errors**: External dependency failures (retryable)
3. **System Errors**: Critical failures (support escalation)

### Error Response Architecture

```csharp
public record MenloError
{
    public required string Code { get; init; }       // Machine-readable error code
    public required string Message { get; init; }    // User-friendly message
    public required string? Details { get; init; }   // Technical details for logging
}

// Domain-specific error types
public static class FinancialErrors
{
    public static MenloError TransactionNotFound(string transactionId) =>
        new() { Code = "FINANCIAL_001", Message = "Transaction not found", Details = $"TransactionId: {transactionId}" };
    
    public static MenloError InvalidAmount(decimal amount) =>
        new() { Code = "FINANCIAL_002", Message = "Invalid transaction amount", Details = $"Amount: {amount}" };
}
```

### Resilience Patterns

**AI Service Resilience**:

- **Circuit Breaker**: Prevent cascade failures when AI services are unavailable
- **Retry with Backoff**: Automatic retry for transient AI processing failures
- **Graceful Degradation**: System remains functional when AI features are unavailable
- **Fallback Strategies**: Default categorization when AI suggestions fail

**Database Resilience**:

- **Connection Pooling**: Efficient database connection management
- **Retry Policies**: Automatic retry for transient database failures
- **Timeout Configuration**: Prevent hanging operations
- **Health Checks**: Continuous monitoring of database availability

### Error Handling Constraints

**CONSTRAINT EH-001**: All external service calls must use circuit breaker pattern
**CONSTRAINT EH-002**: User-facing errors must be actionable and non-technical
**CONSTRAINT EH-003**: All errors must be logged with correlation IDs for debugging
**CONSTRAINT EH-004**: AI service failures cannot break core application functionality
**CONSTRAINT EH-005**: Database operations must use retry policies with exponential backoff

---

## Monitoring, Observability & Operations

### Observability Strategy

**Monitoring Stack**:

- **Application Metrics**: .NET built-in metrics + custom business metrics
- **Logging**: Structured logging with Serilog (local file storage)
- **Health Checks**: Endpoint monitoring for all critical dependencies
- **Performance Tracking**: Request timing and AI processing metrics
- **Dashboard**: Aspire dashboard or Grafana if Aspire not possible

**Key Metrics to Track**:

```csharp
// Business Metrics
- planning_lists_created_per_day
- ai_suggestions_accepted_rate
- budget_variance_alerts_triggered
- bank_import_success_rate

// Technical Metrics  
- api_response_time_95th_percentile
- ollama_inference_duration
- database_connection_pool_usage
- memory_usage_by_ai_models
```

### Health Monitoring Framework

**Health Check Categories**:

1. **Critical**: Database connectivity, authentication service
2. **Important**: AI service availability, file system access
3. **Optional**: External services, non-essential features

**Health Check Implementation**:

```csharp
builder.Services.AddHealthChecks()
    .AddEntityFramework<MenloDbContext>()
    .AddCheck<OllamaHealthCheck>("ollama")
    .AddCheck<AzureAdHealthCheck>("azure-ad");
```

### Operational Procedures

**Deployment Monitoring**:

- Automated health checks post-deployment
- Rollback triggers on health check failures
- Performance regression detection
- Family notification of service disruptions

**Backup & Recovery**:

- Daily automated PostgreSQL backups
- AI model backup (periodic, after retraining)
- Configuration backup (Azure AD settings, feature flags)
- Recovery testing monthly

**Performance Optimization**:

- Weekly performance review of AI inference times
- Monthly database performance analysis
- Quarterly review of resource utilization trends
- Annual hardware capacity planning

### Operational Constraints

**CONSTRAINT OP-001**: Health checks must not impact application performance
**CONSTRAINT OP-002**: All monitoring data stored locally (no cloud analytics)
**CONSTRAINT OP-003**: Backup restoration must be testable and documented
**CONSTRAINT OP-004**: Performance degradation triggers automated alerts
**CONSTRAINT OP-005**: System must be operable by family members (not just developers)

---

## Architecture Implementation Roadmap

### Phase 1: Foundation modelling

- [ ] PostgreSQL migration from Cosmos DB
- [ ] Enhanced authentication with policy-based authorization
- [ ] Ollama integration with basic AI services
- [ ] Core domain model implementation with Result patterns

### Phase 2: Core Features modelling

- [ ] Planning list photo interpretation
- [ ] Budget management with AI categorization
- [ ] Bank transaction import and reconciliation
- [ ] Basic event scheduling

### Phase 3: AI Enhancement modelling

- [ ] Advanced AI suggestions and learning
- [ ] Predictive budget analysis
- [ ] Smart scheduling with conflict detection
- [ ] Maintenance prediction and optimization

### Phase 4: Polish & Family Validation modelling

- [ ] UI/UX refinement based on family feedback
- [ ] Performance optimization
- [ ] Advanced reporting and insights
- [ ] Mobile responsiveness improvements

---

## Architectural Decision Records (ADRs)

This document serves as the master architecture guide. Specific technical decisions are documented in individual ADRs:

- **[ADR-001](adr-001-hosting-strategy.md)**: Hybrid Cloud-Local Hosting Strategy
- **ADR-002** (Future): PostgreSQL Migration Strategy  
- **ADR-003** (Future): AI Model Selection and Local Deployment
- **ADR-004** (Future): Event-Driven Architecture Implementation

---

## Conclusion

This architecture document establishes the foundational principles for building a robust, family-scale, AI-enhanced home management application. The architecture prioritizes:

1. **Family Privacy**: All sensitive data remains local
2. **Cost Consciousness**: Sustainable hosting costs under R365/month
3. **Natural Workflows**: AI enhances rather than replaces human processes
4. **Developer Productivity**: Clear patterns and constraints guide implementation
5. **Future Flexibility**: Extensible design for evolving family needs

The constraints and patterns defined here should guide all implementation decisions, ensuring consistency and maintainability as the application evolves to meet the family's changing needs.

---

*This document represents the authoritative architectural foundation for the Menlo Home Management application. All development should align with the principles, patterns, and constraints defined herein.*
