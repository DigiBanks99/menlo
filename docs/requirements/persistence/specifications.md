# Persistence Layer Requirements

## 1. Overview

The persistence layer is the foundational data storage mechanism for the Menlo Home Management application. It is designed to ensure data sovereignty, privacy, and integrity for the family's personal and financial information. Following the [Architecture Document](../../explanations/architecture-document.md), the system utilizes a hybrid cloud-local approach where the database resides exclusively on the home server.

## 2. Business Requirements

| ID         | Requirement          | Description                                                                                                                                           | Priority     |
| :--------- | :------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------- | :----------- |
| **BR-001** | **Data Sovereignty** | The family must have complete ownership and physical control over their data. All primary data storage must reside on the local home server.          | **Critical** |
| **BR-002** | **Privacy First**    | No Personally Identifiable Information (PII) or financial data shall be stored in external cloud databases or transmitted to third-party AI services. | **Critical** |
| **BR-003** | **Data Integrity**   | The system must ensure the accuracy and consistency of financial and planning data, preventing corruption or accidental loss.                         | **High**     |
| **BR-004** | **Auditability**     | All changes to financial records, budgets, and critical family data must be traceable to a specific user and time.                                    | **High**     |
| **BR-005** | **Resilience**       | The data storage must be resilient to power failures (via journaling/WAL) and support recovery strategies.                                            | **Medium**   |

## 3. Functional Requirements

### 3.1. Database Technology & Structure

| ID         | Requirement               | Acceptance Criteria                                                                                                                                                                                                                                                                                                                              |
| :--------- | :------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **FR-001** | **PostgreSQL Engine**     | The system MUST use PostgreSQL as the primary relational database management system.                                                                                                                                                                                                                                                             |
| **FR-002** | **Schema Separation**     | Data MUST be organized into separate schemas corresponding to the bounded contexts defined in the architecture:<br>- `planning` (Planning & Inventory)<br>- `budget` (Budget Management)<br>- `financial` (Financial Management)<br>- `events` (Event Management)<br>- `household` (Household Management)<br>- `shared` (Cross-cutting concerns) |
| **FR-003** | **Entity Framework Core** | The system MUST use Entity Framework Core as the Object-Relational Mapper (ORM) for data access.                                                                                                                                                                                                                                                 |
| **FR-004** | **Migration Management**  | Database schema changes MUST be managed via versioned EF Core migrations, applied automatically or via CLI during deployment.                                                                                                                                                                                                                    |

### 3.2. Data Management & Security

| ID         | Requirement               | Acceptance Criteria                                                                                                                          |
| :--------- | :------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------- |
| **FR-005** | **Soft Deletes**          | All user-generated entities MUST implement soft-delete functionality (e.g., `IsDeleted` flag) to preserve history and referential integrity. |
| **FR-006** | **Auditing Fields**       | All entities MUST implement the `IAuditable` interface, tracking `CreatedBy`, `CreatedAt`, `ModifiedBy`, and `ModifiedAt`.                   |
| **FR-007** | **Encryption at Rest**    | The database storage volume MUST be encrypted (e.g., via LUKS on the host or PostgreSQL TDE if available/configured).                        |
| **FR-008** | **Encryption in Transit** | All connections to the database MUST use TLS/SSL encryption.                                                                                 |
| **FR-009** | **Connection Pooling**    | The application MUST use connection pooling (e.g., Npgsql pooling) to manage database connections efficiently.                               |

### 3.3. Data Types & Standards

| ID         | Requirement            | Acceptance Criteria                                                                                                                                 |
| :--------- | :--------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------- |
| **FR-010** | **Monetary Values**    | All monetary values MUST be stored with high precision (e.g., `decimal` / `numeric`) to prevent rounding errors.                                    |
| **FR-011** | **UTC Timestamps**     | All datetime values MUST be stored in UTC.                                                                                                          |
| **FR-012** | **Strongly Typed IDs** | The system SHOULD support strongly typed IDs (e.g., GUIDs wrapped in value objects) where applicable in the domain model, mapped to `uuid` columns. |

## 4. Non-Functional Requirements

| ID          | Requirement      | Description                                                                                            | Metric                           |
| :---------- | :--------------- | :----------------------------------------------------------------------------------------------------- | :------------------------------- |
| **NFR-001** | **Performance**  | Simple read/write operations by ID should be fast to ensure a responsive UI.                           | < 50ms execution time            |
| **NFR-002** | **Scalability**  | The database design should support the target scale of a single family with 5+ years of history.       | Support > 1M transaction records |
| **NFR-003** | **Availability** | The database must be available 24/7 on the home server, recovering automatically after system reboots. | 99.9% Uptime (Local)             |
| **NFR-004** | **Backup**       | Automated daily backups must be performed to a separate local storage location.                        | RPO < 24 hours                   |

## 5. Constraints

- **C-001**: No cloud-managed databases (e.g., Azure SQL, AWS RDS).
- **C-002**: No direct database access from the frontend; all access must go through the API.
- **C-003**: The database runs in a containerized environment (Docker/Podman).

## 6. Assumptions

- The home server has sufficient storage (SSD) and RAM to host the PostgreSQL instance alongside the AI models.
- The family understands that physical damage to the home server requires restoration from backups (which should ideally be off-site, though out of scope for this specific document).
