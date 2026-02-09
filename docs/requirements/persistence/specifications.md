# Persistence Layer Specification

## 1. Overview

The persistence layer provides the foundational data storage for the Menlo Home Management application. Following the hybrid cloud-local architecture, the database resides exclusively on the home server, ensuring complete data sovereignty and privacy.

This specification is designed to enable atomic implementation tasks.

---

## 2. Business Requirements

| ID         | Requirement          | Description                                                                                                      | Priority     |
| :--------- | :------------------- | :--------------------------------------------------------------------------------------------------------------- | :----------- |
| **BR-001** | **Data Sovereignty** | All primary data storage must reside on the local home server. The family has complete ownership of their data. | **Critical** |
| **BR-002** | **Privacy First**    | No PII or financial data stored in external cloud databases or transmitted to third-party AI services.          | **Critical** |
| **BR-003** | **Data Integrity**   | Financial and planning data must be accurate and consistent, preventing corruption or accidental loss.          | **High**     |
| **BR-004** | **Auditability**     | All changes to financial records, budgets, and critical data must be traceable to a specific user and time.     | **High**     |
| **BR-005** | **Resilience**       | Data storage must be resilient to power failures and support recovery strategies.                               | **Medium**   |

---

## 3. Technical Stack

| Component       | Technology                        | Notes                                        |
| :-------------- | :-------------------------------- | :------------------------------------------- |
| **Database**    | PostgreSQL 16+                    | Running in Podman container on WSL2          |
| **ORM**         | Entity Framework Core 9+          | Code-first with migrations                   |
| **Connection**  | Npgsql                            | With connection pooling                      |
| **Container**   | Podman                            | On Windows 10 Home with WSL2                 |
| **Data Volume** | Podman named volume               | Persistent storage for PostgreSQL data       |

---

## 4. Database Schema Design

### 4.1 Schema Separation

Data is organized into separate PostgreSQL schemas corresponding to bounded contexts:

| Schema       | Bounded Context        | Primary Aggregates                       |
| :----------- | :--------------------- | :--------------------------------------- |
| `budget`     | Budget Management      | Budget, BudgetCategory, BudgetAllocation |
| `planning`   | Planning & Inventory   | PlanningList, ListItem, PantryItem       |
| `household`  | Household Management   | Household, Person, Appliance, UtilityAccount |
| `financial`  | Financial Management   | FinancialAccount, Transaction, IncomeSource |
| `events`     | Event Management       | Event, RecurringEvent                    |
| `shared`     | Cross-cutting Concerns | Lookup tables, configuration             |

### 4.2 Naming Conventions

Follow PostgreSQL conventions:

| Element         | Convention                  | Example                          |
| :-------------- | :-------------------------- | :------------------------------- |
| **Tables**      | `snake_case`, plural        | `budget_categories`              |
| **Columns**     | `snake_case`                | `created_at`, `modified_by`      |
| **Primary Keys**| `id`                        | `id` (uuid type)                 |
| **Foreign Keys**| `{referenced_table}_id`     | `budget_id`, `person_id`         |
| **Indexes**     | `ix_{table}_{columns}`      | `ix_transactions_category_date`  |
| **Constraints** | `{type}_{table}_{columns}`  | `uq_persons_email`, `ck_money_positive` |

---

## 5. DbContext Strategy

### 5.1 Single Context with Schema Separation

Use a single `MenloDbContext` with schema separation per bounded context:

```csharp
public class MenloDbContext : DbContext
{
    // Budget schema
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();

    // Planning schema
    public DbSet<PlanningList> PlanningLists => Set<PlanningList>();

    // ... other DbSets
}
```

### 5.2 Entity Configuration

Each entity uses `IEntityTypeConfiguration<T>` with explicit schema:

```csharp
public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets", "budget");
        // ... column mappings
    }
}
```

### 5.3 Rationale

- **Simplicity**: Single context avoids cross-context transaction complexity
- **Schema Isolation**: Logical separation maintained via PostgreSQL schemas
- **Future Flexibility**: Can split into read contexts if CQRS needed later

---

## 6. Soft Delete Implementation

### 6.1 Requirements

| ID         | Requirement                | Acceptance Criteria                                                    |
| :--------- | :------------------------- | :--------------------------------------------------------------------- |
| **SD-001** | **Soft Delete Flag**       | All user-generated entities have `is_deleted` boolean column           |
| **SD-002** | **Deleted Timestamp**      | Track `deleted_at` (UTC) and `deleted_by` (UserId) when soft-deleted   |
| **SD-003** | **Global Query Filter**    | Soft-deleted records excluded from all queries by default              |
| **SD-004** | **Admin Query Access**     | Ability to query deleted records for audit/history views               |
| **SD-005** | **Cascade Soft Delete**    | Deleting a parent soft-deletes related children                        |

### 6.2 Interface Definition

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    UserId? DeletedBy { get; }

    void SoftDelete(IAuditStampFactory factory);
    void Restore();
}
```

### 6.3 Global Query Filter

```csharp
// In entity configuration
builder.HasQueryFilter(e => !e.IsDeleted);

// To include deleted records
context.Budgets.IgnoreQueryFilters().Where(b => b.IsDeleted);
```

### 6.4 Cascade Soft Delete

Implemented via `SaveChangesInterceptor`:
- When parent is soft-deleted, interceptor identifies related children via navigation properties
- Children are marked as soft-deleted in the same transaction
- Tracks cascade chain to prevent infinite loops

---

## 7. Auditing Implementation

### 7.1 Requirements

| ID         | Requirement            | Acceptance Criteria                                                         |
| :--------- | :--------------------- | :-------------------------------------------------------------------------- |
| **AU-001** | **Created Fields**     | All entities track `created_by` (UserId) and `created_at` (UTC timestamp)  |
| **AU-002** | **Modified Fields**    | All entities track `modified_by` (UserId) and `modified_at` (UTC timestamp)|
| **AU-003** | **Automatic Stamping** | Audit fields populated automatically on SaveChanges                        |
| **AU-004** | **User Context**       | Current user resolved via `IAuditStampFactory` (injected)                  |

### 7.2 Existing Domain Abstractions

The domain layer already defines:
- `IAuditable` interface with `Audit(factory, operation)` method
- `AuditStamp` value object with `ActorId`, `Timestamp`, `CorrelationId`
- `IAuditStampFactory` for creating stamps with current user context
- `AuditOperation` enum (`Create`, `Update`)

### 7.3 EF Core Integration

```csharp
public class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly IAuditStampFactory _stampFactory;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return ValueTask.FromResult(result);

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            var operation = entry.State switch
            {
                EntityState.Added => AuditOperation.Create,
                EntityState.Modified => AuditOperation.Update,
                _ => (AuditOperation?)null
            };

            if (operation.HasValue)
            {
                entry.Entity.Audit(_stampFactory, operation.Value);
            }
        }

        return ValueTask.FromResult(result);
    }
}
```

### 7.4 Column Mapping

```csharp
builder.Property(e => e.CreatedBy)
    .HasColumnName("created_by")
    .HasConversion(v => v!.Value.Value, v => new UserId(v));

builder.Property(e => e.CreatedAt)
    .HasColumnName("created_at");
```

---

## 8. Concurrency Control

### 8.1 Strategy

Use **optimistic concurrency** with row versioning:

| ID         | Requirement              | Acceptance Criteria                                              |
| :--------- | :----------------------- | :--------------------------------------------------------------- |
| **CC-001** | **Row Version Column**   | Entities requiring concurrency have `xmin` system column or explicit `row_version` |
| **CC-002** | **Conflict Detection**   | `DbUpdateConcurrencyException` thrown on concurrent modification |
| **CC-003** | **Per-Entity Decision**  | Concurrency control applied to aggregates handling concurrent updates |

### 8.2 Implementation

```csharp
// Using PostgreSQL xmin system column (recommended)
builder.UseXminAsConcurrencyToken();

// OR explicit row version
builder.Property<uint>("RowVersion")
    .HasColumnName("row_version")
    .IsRowVersion();
```

### 8.3 Entities Requiring Concurrency Control

Defer to implementation phase. Likely candidates:
- `Budget` (husband/wife may update allocations)
- `PlanningList` (collaborative editing)
- `Transaction` (reconciliation updates)

---

## 9. Data Types and Standards

### 9.1 Monetary Values

| ID         | Requirement            | Acceptance Criteria                                                    |
| :--------- | :--------------------- | :--------------------------------------------------------------------- |
| **DT-001** | **Precision**          | All monetary values stored as `numeric(19,4)` for cent-level precision |
| **DT-002** | **Currency Column**    | Monetary columns paired with 3-char ISO currency code                  |
| **DT-003** | **Money Value Object** | EF Core converts `Money` value object to/from amount + currency columns|

```csharp
builder.OwnsOne(e => e.Amount, money =>
{
    money.Property(m => m.Amount)
        .HasColumnName("amount")
        .HasPrecision(19, 4);
    money.Property(m => m.Currency)
        .HasColumnName("currency")
        .HasMaxLength(3);
});
```

### 9.2 Timestamps

| ID         | Requirement        | Acceptance Criteria                                      |
| :--------- | :----------------- | :------------------------------------------------------- |
| **DT-004** | **UTC Storage**    | All timestamps stored as `timestamptz` (timestamp with time zone) |
| **DT-005** | **DateTimeOffset** | C# uses `DateTimeOffset` for all temporal values         |

### 9.3 Strongly Typed IDs

| ID         | Requirement         | Acceptance Criteria                                            |
| :--------- | :------------------ | :------------------------------------------------------------- |
| **DT-006** | **UUID Storage**    | Strongly typed IDs map to PostgreSQL `uuid` columns            |
| **DT-007** | **Value Converter** | Custom `ValueConverter` for each ID type                       |

```csharp
builder.Property(e => e.Id)
    .HasConversion(
        id => id.Value,
        value => new BudgetId(value))
    .HasColumnName("id");
```

---

## 10. Connection and Pooling

### 10.1 Requirements

| ID         | Requirement              | Acceptance Criteria                                        |
| :--------- | :----------------------- | :--------------------------------------------------------- |
| **CP-001** | **Connection Pooling**   | Npgsql connection pooling enabled with appropriate limits  |
| **CP-002** | **Pool Size**            | Min 2, Max 20 connections (single family usage)            |
| **CP-003** | **Connection Timeout**   | 30 second timeout for connection acquisition               |
| **CP-004** | **SSL/TLS**              | Connections use SSL when configured                        |

### 10.2 Connection String Template

```
Host=localhost;Port=5432;Database=menlo;Username=menlo_app;Password=***;
Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Idle Lifetime=300;
SSL Mode=Prefer;Trust Server Certificate=true
```

---

## 11. Migration Strategy

### 11.1 Requirements

| ID         | Requirement              | Acceptance Criteria                                           |
| :--------- | :----------------------- | :------------------------------------------------------------ |
| **MG-001** | **Code-First**           | All schema changes via EF Core migrations                     |
| **MG-002** | **Versioned Migrations** | Migrations versioned and stored in source control             |
| **MG-003** | **CD Integration**       | Migrations applied as part of deployment pipeline             |
| **MG-004** | **Failure Blocks Deploy**| Failed migration prevents application deployment              |
| **MG-005** | **Idempotent**           | Migration scripts can be re-run safely                        |

### 11.2 Migration Commands

```bash
# Create migration
dotnet ef migrations add <MigrationName> --project src/Menlo.Persistence

# Apply migrations (CD pipeline)
dotnet ef database update --project src/Menlo.Persistence

# Generate idempotent script
dotnet ef migrations script --idempotent --output migrations.sql
```

### 11.3 CD Pipeline Integration

```yaml
# Example pipeline step
- name: Apply Database Migrations
  run: |
    dotnet ef database update --project src/Menlo.Persistence
  env:
    ConnectionStrings__MenloDb: ${{ secrets.DB_CONNECTION }}
  continue-on-error: false  # Blocks deployment on failure
```

---

## 12. Seed Data Strategy

### 12.1 Requirements

| ID         | Requirement            | Acceptance Criteria                                          |
| :--------- | :--------------------- | :----------------------------------------------------------- |
| **SE-001** | **Fluent Pattern**     | Seed data defined using fluent builder pattern               |
| **SE-002** | **Migration Embedded** | Seed data applied as part of migrations                      |
| **SE-003** | **Idempotent**         | Seed data can be re-applied without duplicates               |
| **SE-004** | **Minimal MVP Data**   | Only essential lookup/config data seeded                     |

### 12.2 Initial Seed Data

| Schema     | Data                                              |
| :--------- | :------------------------------------------------ |
| `shared`   | Default currency (ZAR), system configuration      |
| `budget`   | Default budget category templates (optional)      |
| `household`| Initial household record for family               |

### 12.3 Implementation Pattern

```csharp
public static class SeedDataExtensions
{
    public static void SeedInitialData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Currency>().HasData(
            new { Code = "ZAR", Name = "South African Rand", Symbol = "R" }
        );
    }
}
```

---

## 13. Backup Strategy

### 13.1 Requirements

| ID         | Requirement            | Acceptance Criteria                                         |
| :--------- | :--------------------- | :---------------------------------------------------------- |
| **BK-001** | **Daily Backups**      | Automated daily backup of entire database                   |
| **BK-002** | **Local Storage**      | Backups stored on external HDD                              |
| **BK-003** | **Cloud Sync**         | Backups synced to OneDrive for off-site storage             |
| **BK-004** | **Retention Policy**   | Keep 7 daily, 4 weekly, 12 monthly backups                  |
| **BK-005** | **Pre-Upgrade Backup** | Mandatory backup before PostgreSQL container updates        |

### 13.2 Environment

- **Host OS**: Windows 10 Home
- **Container Runtime**: Podman on WSL2
- **Data Volume**: Podman named volume (`menlo_postgres_data`)
- **Backup Location**: External HDD (e.g., `E:\Backups\menlo`)
- **Cloud Sync**: OneDrive folder for long-term retention

### 13.3 Backup Script (Template)

```bash
#!/bin/bash
# backup-menlo-db.sh - Run from WSL2

BACKUP_DIR="/mnt/e/Backups/menlo"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/menlo_backup_$TIMESTAMP.sql.gz"

# Create backup directory if not exists
mkdir -p "$BACKUP_DIR"

# Run pg_dump inside container and compress
podman exec menlo-postgres pg_dump -U menlo_app menlo | gzip > "$BACKUP_FILE"

# Verify backup
if [ -s "$BACKUP_FILE" ]; then
    echo "Backup successful: $BACKUP_FILE"
else
    echo "Backup failed!" >&2
    exit 1
fi

# Cleanup old backups (keep last 7 daily)
find "$BACKUP_DIR" -name "menlo_backup_*.sql.gz" -mtime +7 -delete
```

### 13.4 Scheduling

Use Windows Task Scheduler to run WSL command:

```
wsl -d Ubuntu -e /path/to/backup-menlo-db.sh
```

Schedule: Daily at 02:00 AM

### 13.5 Restoration Procedure

```bash
# Stop application containers
podman stop menlo-api

# Restore from backup
gunzip -c /mnt/e/Backups/menlo/menlo_backup_YYYYMMDD.sql.gz | \
    podman exec -i menlo-postgres psql -U menlo_app menlo

# Restart application
podman start menlo-api
```

### 13.6 Container Upgrade Procedure

```bash
# 1. Create pre-upgrade backup
./backup-menlo-db.sh

# 2. Stop and remove old container (keeps volume)
podman stop menlo-postgres
podman rm menlo-postgres

# 3. Pull new image
podman pull docker.io/library/postgres:17

# 4. Start new container with same volume
podman run -d --name menlo-postgres \
    -v menlo_postgres_data:/var/lib/postgresql/data \
    -e POSTGRES_USER=menlo_app \
    -e POSTGRES_DB=menlo \
    postgres:17

# 5. Verify data integrity
podman exec menlo-postgres psql -U menlo_app -c "SELECT count(*) FROM budget.budgets;"
```

---

## 14. Security

### 14.1 Encryption Requirements

| ID         | Requirement              | Acceptance Criteria                                       |
| :--------- | :----------------------- | :-------------------------------------------------------- |
| **SC-001** | **Encryption at Rest**   | Database volume encrypted (LUKS on WSL2 or BitLocker on host) |
| **SC-002** | **Encryption in Transit**| SSL/TLS for database connections when exposed             |
| **SC-003** | **No Direct Access**     | Database not exposed outside localhost; API only access   |

### 14.2 Access Control

| ID         | Requirement              | Acceptance Criteria                                       |
| :--------- | :----------------------- | :-------------------------------------------------------- |
| **SC-004** | **Application User**     | Dedicated `menlo_app` PostgreSQL user with limited privileges |
| **SC-005** | **No Superuser**         | Application never connects as `postgres` superuser        |
| **SC-006** | **Schema Permissions**   | User has CRUD on application schemas only                 |

---

## 15. Non-Functional Requirements

| ID          | Requirement      | Target                                  | Notes                              |
| :---------- | :--------------- | :-------------------------------------- | :--------------------------------- |
| **NFR-001** | **Read Latency** | < 50ms for simple queries by ID         | Indexed lookups                    |
| **NFR-002** | **Write Latency**| < 100ms for single entity operations    | Including audit stamping           |
| **NFR-003** | **Capacity**     | 5+ years of family data (~1M+ records)  | Transactions, events, history      |
| **NFR-004** | **Availability** | 99.9% uptime on home server             | Auto-restart on reboot             |
| **NFR-005** | **RPO**          | < 24 hours (daily backups)              | Recovery Point Objective           |

---

## 16. Integration Events (Future)

### 16.1 Current Implementation

**In-memory event bus** for domain/integration events:
- Events dispatched after `SaveChanges` completes
- Handlers run in same process
- Suitable for single-server deployment

### 16.2 Future Enhancement (Low Priority)

| ID         | Requirement        | Description                                                |
| :--------- | :----------------- | :--------------------------------------------------------- |
| **IE-001** | **Outbox Pattern** | Persist events to `shared.outbox` table before publishing  |
| **IE-002** | **At-Least-Once**  | Guarantee event delivery via polling/retry                 |
| **IE-003** | **Idempotency**    | Handlers must be idempotent for replay scenarios           |

Trigger: Implement if performance degrades or reliability issues emerge with in-memory bus.

---

## 17. Constraints

| ID        | Constraint                                                              |
| :-------- | :---------------------------------------------------------------------- |
| **C-001** | No cloud-managed databases (Azure SQL, AWS RDS, etc.)                   |
| **C-002** | No direct database access from frontend; all access via API             |
| **C-003** | Database runs in Podman container on WSL2                               |
| **C-004** | Single family usage (not multi-tenant)                                  |

---

## 18. Assumptions

| ID        | Assumption                                                                          |
| :-------- | :---------------------------------------------------------------------------------- |
| **A-001** | Home server has sufficient SSD storage and RAM for PostgreSQL alongside AI models  |
| **A-002** | External HDD available for local backups                                            |
| **A-003** | OneDrive available for cloud backup sync                                            |
| **A-004** | Family understands physical damage requires restoration from backups                |
| **A-005** | WSL2 and Podman are stable on the development/production machine                    |

---

## 19. Implementation Tasks Breakdown

This specification enables the following atomic implementation tasks:

### Phase 1: Foundation
- [ ] Create `Menlo.Persistence` project with EF Core and Npgsql
- [ ] Implement `MenloDbContext` with schema configuration
- [ ] Create base entity configurations (audit columns, soft delete)
- [ ] Implement `ISoftDeletable` interface and base class
- [ ] Implement `AuditingInterceptor` for automatic audit stamping
- [ ] Implement `SoftDeleteInterceptor` for cascade soft delete
- [ ] Create `ValueConverter` classes for strongly typed IDs
- [ ] Create `ValueConverter` for `Money` value object

### Phase 2: Schema Implementation
- [ ] Create `budget` schema entity configurations
- [ ] Create `planning` schema entity configurations
- [ ] Create `household` schema entity configurations
- [ ] Create `financial` schema entity configurations
- [ ] Create `events` schema entity configurations
- [ ] Create `shared` schema entity configurations

### Phase 3: Migrations & Seeding
- [ ] Create initial migration with all schemas
- [ ] Implement seed data for essential lookup tables
- [ ] Configure migration in CD pipeline
- [ ] Create migration verification tests

### Phase 4: Backup & Operations
- [ ] Create backup script for WSL2/Podman
- [ ] Configure Windows Task Scheduler for daily backups
- [ ] Document restoration procedure
- [ ] Document container upgrade procedure
- [ ] Set up OneDrive sync for backup folder

### Phase 5: Testing
- [ ] Create integration tests for DbContext
- [ ] Test soft delete with cascade
- [ ] Test audit stamping
- [ ] Test concurrency conflict handling
- [ ] Test backup and restore procedure
