# Domain Model Design

- [Domain Model Design](#domain-model-design)
  - [Architecture](#architecture)
    - [Cross-Aggregate Integration Strategy](#cross-aggregate-integration-strategy)
    - [AI Learning State Management](#ai-learning-state-management)
    - [Attribution Complexity Resolution](#attribution-complexity-resolution)
  - [Aggregate Design](#aggregate-design)
    - [Budget Aggregate](#budget-aggregate)
    - [Planning \& Inventory Aggregate](#planning--inventory-aggregate)
    - [Household Bounded Context](#household-bounded-context)
      - [Household Configuration Aggregate](#household-configuration-aggregate)
      - [Utility Account Aggregate](#utility-account-aggregate)
    - [Financial Aggregate](#financial-aggregate)
    - [Event Aggregate](#event-aggregate)
  - [Integration Event Design](#integration-event-design)
    - [Core Integration Events](#core-integration-events)
    - [Cross-Aggregate Processing](#cross-aggregate-processing)
  - [Domain Services](#domain-services)
    - [Attribution Calculator (Shared)](#attribution-calculator-shared)
  - [Template Learning Strategy](#template-learning-strategy)
  - [AI Service Interfaces (Purpose-Driven Naming)](#ai-service-interfaces-purpose-driven-naming)
    - [Planning \& Inventory Domain](#planning--inventory-domain)
    - [Budget Domain](#budget-domain)
    - [Event Domain](#event-domain)
    - [Household Domain](#household-domain)
    - [Financial Domain](#financial-domain)
  - [Key Design Principles Applied](#key-design-principles-applied)

## Architecture

### Cross-Aggregate Integration Strategy

- **Read Models**: Cross-aggregate queries use projections (no direct aggregate dependencies)
- **Integration Events**: Aggregates communicate via domain events for write operations
- **Eventual Consistency**: Changes propagate asynchronously between bounded contexts

### AI Learning State Management

- **Domain State**: User corrections stored as domain events in aggregates
- **AI Learning State**: Vector embeddings and model fine-tuning data in separate vector store (Qdrant/Chroma)
- **Storage Strategy**: PostgreSQL for domain events, Vector store for AI embeddings, Local files for Ollama model data

### Attribution Complexity Resolution

- **Attribution Domain Service**: Centralized business rules for personal vs rental splits
- **Reusable**: Used across Budget, Financial, and Utility aggregates
- **Testable**: Business logic isolated and unit testable

## Aggregate Design

### Budget Aggregate

**Root**: Budget

**Entities**: BudgetCategory, BudgetAllocation, BudgetRealization, BudgetTransaction

**Value Objects**: Money, AttributionRule, BudgetPeriod, CategoryPath

**Category Structure Examples**:

```tree
Income/
├── [Your Name]/
│   ├── Employment/
│   │   ├── Gross Salary
│   │   ├── Bonuses
│   │   └── Net Employment Income
│   └── Investment Returns/
│       ├── Dividend Income
│       └── Interest Income
└── Household Income/
    ├── Rental Income/
    │   └── Student Rental (What-if)
    └── Savings Growth/
        └── Bank Interest

Housing/
├── Bond & Insurance
├── Municipal Services/
│   ├── Rates & Levies
│   ├── Electricity
│   ├── Water & Refuse
│   └── Sewerage
└── Security & Safety/
    ├── Armed Response
    └── Security System

Transport/
├── Petrol & Fuel
├── Car Insurance
├── Vehicle Maintenance
└── Vehicle Licensing

Family Living/
├── Groceries & Household
├── Clothing/
│   ├── [Child 1 Name]
│   ├── [Child 2 Name]
│   └── [Your name]
└── Entertainment & Social/
    └── Parties & Events/
        ├── [Child 1]
        └── [Child 2]
```

**Budget Structure**:

- **Annual Budget with Monthly Divisions**: Main budget spans full year with monthly breakdowns
- **Hierarchical Categories**: Unlimited nesting (Housing → Municipal Services → Electricity)
- **Leaf-Level Planning**: Allocations primarily at concrete leaf categories based on experience
- **Unified Income/Expense**: Single hierarchy containing both income and expense categories
- **Category Carryover**: Annual budget carried over from previous year as starting point

**Budget States**:

```sh
Planned → Realized → Spent
```

- **Planned**: Annual leaf-level allocations based on experience (subject to major change)
- **Realized**: Planning lists processed into concrete, trackable budget items (more stable)
- **Spent**: Actual transactions from bank statements/receipts

**Invariants**:

- Attribution percentages must sum to 100%
- Budget categories maintain hierarchical integrity (parent-child relationships)
- **No spending constraints**: Over-budget spending allowed with notifications
- **No workflow blocking**: Planning lists never prevented, budget realization warnings only
- Leaf categories can have direct allocations independent of parent totals
- Parent category is the sum of children
- All transactions and budget items must have attribution classification (Family, Rental, or Shared)

**Core Behaviors**:

- **Annual Creation**: Create new year budget from previous year template
- **Leaf-Level Allocation**: Set specific amounts on concrete categories (Electricity, Groceries, etc.)
- **Manual Realization**: User-controlled conversion of planned items to realized budget items
- **Planning List Integration**: Suggest budget impacts and new categories without blocking workflow
- **Flexible Category Management**: Create new categories as needed, modify existing allocations
- **Attribution Extraction**: Generate rental vs personal cost reports for tax purposes
- **Ephemeral What-If Scenarios**: Rental income scenarios without permanent budget changes

**AI Integration**:

- `BudgetAnalyser`: Side-by-side budget vs actual analysis, variance detection, over-budget alerts, insights into where spending can be limited or reallocation can be made
- `TransactionCategorizer`: Best-guess mapping of planning items to categories, learns from corrections
- `RentalCostAnalyser`: Extracts rental-attributable costs for tax reporting

**Integration Events Consumed**:

- `PlanningListCreated`: Analyses budget impact and suggests categories (non-blocking)
- `PlanningListRealized`: Creates budget realizations from planning items into budget realizations using best-guess categorization

**Key Workflows**:

1. **Annual Budget Setup**:
   - Copy previous year's category structure and amounts and carry-over remaining surplus or debt
   - Adjust amounts based on experience and expected changes
   - Chat with spouse about variable categories (clothing, holidays, gifts)
   - Divide annual amounts into monthly allocations

2. **Planning List Integration**:
   - Receive `PlanningListRealized` events
   - Use AI to map items to best-matching categories
   - Create realizations even if categories don't exist (suggest new ones)
   - Allow over-budget with notifications and reallocation suggestions

3. **Budget Realization Process**:
   - User manually reviews planning list budget suggestions
   - Converts planned budget items to realized (more concrete/trackable)
   - Can modify budget allocations during realization
   - Warnings about over-budget situations (but not blocking)

4. **Temporal Budget Views**:
   - **Past months**: Default to Spent view, can toggle to see what was Planned/Realized
   - **Current month**: Visual progress toward budget (Planned vs Current Spent)
   - **Future months**: Planned and Realized side-by-side

5. **Rental Cost Extraction**:
   - Apply attribution rules to extract personal vs rental costs
   - Generate tax-ready reports with proper categorization
   - Support ephemeral what-if scenarios for rental income modelling

### Planning & Inventory Aggregate

**Root**: PlanningList

**Entities**: ListItem, ListTemplate, PantryItem, Recipe, MealPlan

**Value Objects**: CostEstimate, ListType, ListState, Quantity, ProductId, Ingredient

**Primary Focus**: **Planning workflows and inventory management** with meal planning integration

**Workflow States**:

```sh
Created → PhotoCaptured → AIProcessed → UserReviewed → Realized
```

**Pantry Management**:

- **Inventory Tracking**: Current stock levels and expiration dates
- **Meal Planning Integration**: Recipe-driven shopping list generation  
- **Smart Replenishment**: Automatic low-stock detection and shopping suggestions
- **Waste Reduction**: Expiration tracking with usage recommendations

**Invariants**:

- List items can have optional cost estimates
- Lists can have total cost estimates even with some items unpriced
- Lists must have total budget estimate to be realized (manual entry required)
- Template instances maintain reference to source template (optional - detected patterns)
- Cannot realize without total budget estimate

**AI Integration**:

- `ListPhotoInterpreter`: Processes handwritten list photos into appropriate planning item types
- `BudgetImpactAnalyser`: Analyses budget impact before realization (works with all planning item types)
- `PantryOptimizer`: Suggests meals and generates `PantryPlanningItem` based on inventory and expiration dates
- `ShoppingListGenerator`: Creates lists with `PantryPlanningItem` from meal plans and pantry gaps
- `MaintenanceScheduler`: Creates `MaintenancePlanningItem` from appliance maintenance requirements
- `SmartPlanningItemCreator`: Determines appropriate planning item type based on context (basic, pantry, maintenance, etc.)
- Template learning: Automatic pattern detection across all planning item types

**Pantry Management Within Planning**:

- **Inventory Tracking**: Current stock levels and expiration dates managed through `PantryItem` entities
- **Recipe Management**: Store family recipes with ingredient requirements in `Recipe` entities  
- **Meal Planning Integration**: `MealPlan` entities generate `PantryPlanningItem` for shopping lists
- **Smart Replenishment**: Automatic low-stock detection creates `PantryPlanningItem` with urgency
- **Waste Reduction**: Expiration tracking generates meal suggestions and urgent `PantryPlanningItem`
- **Clean Separation**: Pantry data separate from basic planning, connected only when needed through specialized item types

**Budget Estimate Rules**:

- **Manual Entry**: User provides total budget estimate for list
- **AI Suggestion**: AI suggests estimate based on similar historical lists
- **Validation**: Total estimate required before realization allowed

**Integration Events Published**:

- `PlanningListRealized`: Contains list of polymorphic PlanningItems (BasicPlanningItem, PantryPlanningItem, MaintenancePlanningItem, etc.) for other aggregates to process

**Realization Process**:

- **Trigger**: Manual "Realize" button click
- **Validation**: Total budget estimate must exist
- **Budget Impact**: Allow over-budget with warnings and suggestions
- **Event Publishing**: Single `PlanningListRealized` event with PlanningItems

**PlanningItem Structure**:

```csharp
// Base planning item - clean and simple
public abstract record PlanningItem(
    string Description,
    Money? EstimatedCost,      // Optional - individual items may not have costs
    DateOnly? PlannedDate,     // Optional - some items not time-bound
    string? Notes,             // Additional context from list
    PlanningItemId Id          // Traceability
);

// Specialized planning items through inheritance
public record BasicPlanningItem(
    string Description,
    Money? EstimatedCost,
    DateOnly? PlannedDate,
    string? Notes,
    PlanningItemId Id
) : PlanningItem(Description, EstimatedCost, PlannedDate, Notes, Id);

public record PantryPlanningItem(
    string Description,
    Money? EstimatedCost,
    DateOnly? PlannedDate,
    string? Notes,
    PlanningItemId Id,
    RecipeId? ForRecipe,       // Specialized pantry information
    DateOnly? UseByDate,
    PantryUrgency Urgency
) : PlanningItem(Description, EstimatedCost, PlannedDate, Notes, Id);

public record MaintenancePlanningItem(
    string Description,
    Money? EstimatedCost,
    DateOnly? PlannedDate,
    string? Notes,
    PlanningItemId Id,
    ApplianceId ApplianceId,   // Specialized maintenance information
    MaintenanceType MaintenanceType,
    MaintenancePriority Priority
) : PlanningItem(Description, EstimatedCost, PlannedDate, Notes, Id);
```

**Core Behaviors**:

- **Basic Planning Lists**: Simple shopping/task lists with basic planning items
- **Pantry-Driven Planning**: Generate `PantryPlanningItem` from meal plans and inventory needs
- **Maintenance Planning**: Create `MaintenancePlanningItem` from appliance maintenance requirements
- **Smart List Generation**: AI creates appropriate planning item types based on context
- **Flexible Realization**: All planning item types can be realized regardless of specialization
- **Type-Aware Processing**: Downstream aggregates can pattern match on planning item types for specialized handling

### Household Bounded Context

**Two aggregates within single bounded context for clean separation of concerns**:

#### Household Configuration Aggregate

**Root**: Household

**Entities**: Person, Appliance

**Value Objects**: PersonId, ApplianceSpec, MaintenanceSchedule

**Primary Focus**: **Family structure and appliance registry** with configurable maintenance scheduling

**Invariants**:

- Each person must have unique identification within household
- Appliances can optionally have maintenance schedules configured
- Not all appliances require maintenance tracking
- Maintenance schedules are appliance-specific and configurable

**Responsibilities**:

- Family member registry for event attribution
- Appliance inventory with optional maintenance configuration
- Configurable maintenance requirements per appliance type
- Publishing maintenance requirement events when due

**Core Behaviors**:

- **Person Management**: Register family members for event attribution (not utility usage tracking)
- **Appliance Registry**: Track make, model, purchase date, warranty info
- **Optional Maintenance Configuration**: Configure maintenance schedules per appliance (washing machine, oven, dishwasher, etc.)
- **Maintenance Requirement Detection**: Determine what maintenance is needed based on configured schedules
- **Flexible Maintenance Scheduling**: Some appliances have maintenance, others don't - fully configurable

**AI Integration**:

- `ApplianceMaintenanceRequirements`: Determines what maintenance is needed and base frequency for configured appliances

**Integration Events Published**:

- `ApplianceMaintenanceRequired`: Contains appliance maintenance needs with criticality and timing requirements (only for appliances with configured maintenance)

#### Utility Account Aggregate

**Root**: UtilityAccount

**Entities**: UtilityReading, UtilityInvoice

**Value Objects**: Reading, UsageCalculation, RentalAttribution

**Primary Focus**: **South African utility tracking** with rental vs house attribution

**Account Types and SA Context**:

- **Electricity**: Prepaid system - track top-ups and usage patterns
- **Water**: Monthly meter readings - calculate usage between readings
- **Municipal Rates**: Separate invoice from City of Tshwane (rates, levies, refuse, sanitization)
- **Gas**: Refill tracking every 6-8 months with cost attribution
- **Fibre Internet**: Monthly billing with potential rental attribution

**Invariants**:

- Utility readings must be chronologically ordered
- Rental attribution percentages must be consistent across time periods
- Gas refill tracking must account for long intervals between refills
- Municipal invoices are separate from usage-based utilities

**Responsibilities**:

- Track SA-specific utility structures (prepaid electricity, monthly water, separate municipal invoices)
- **Rental vs House Attribution**: Primary concern for tax reporting and rental income modelling
- **Retrospective Attribution Analysis**: Analyze bill changes to determine rental impact
- Usage pattern analysis for efficiency recommendations
- Municipal service cost tracking (rates, levies, refuse, sanitization)

**Core Behaviors**:

- **Prepaid Electricity Tracking**: Track top-ups, usage patterns, and rental attribution
- **Water Usage Calculation**: Monthly meter readings with usage calculation and rental attribution
- **Municipal Invoice Management**: Separate tracking for rates, levies, refuse, sanitization with rental splits
- **Gas Refill Tracking**: Long-interval tracking (6-8 months) with cost attribution
- **Fibre Internet Attribution**: Monthly costs with potential rental percentage
- **Retrospective Rental Analysis**: Analyze usage pattern changes when rental situation changes

**AI Integration**:

- `UtilityOptimizer`: Efficiency tracking and recommendations based on usage patterns
- `RentalAttributionAnalyser`: Analyses bill changes to suggest rental vs house attribution percentages

**Integration Events Published**:

- `UtilityUsageRecorded`: For budget impact and financial reconciliation

### Financial Aggregate

**Root**: FinancialAccount

**Entities**: Transaction, IncomeSource, BankImport, TransactionReconciliation

**Value Objects**: TransactionCategory, Attribution, Money, ImportBatch, ReconciliationStatus, TransactionType

**Primary Focus**: **Financial transaction management and bank reconciliation** with AI-powered categorization and attribution

**Account Types**:

- **Checking Accounts**: Day-to-day transaction accounts
- **Savings Accounts**: Interest-bearing savings with growth tracking  
- **Investment Accounts**: Dividends, capital gains, and investment tracking
- **Credit Accounts**: Credit card and loan tracking

**Transaction Flow States**:

```sh
Imported/Manual → Categorized → Attributed → Reconciled → Budgeted
```

- **Imported/Manual**: Raw transactions from bank imports or manual entry
- **Categorized**: AI-suggested budget category mapping with user confirmation
- **Attributed**: Personal vs rental and family member attribution applied
- **Reconciled**: Matched against budget realizations and planning items
- **Budgeted**: Linked to budget transactions for variance analysis

**Invariants**:

- Transactions must balance (no orphaned money)
- Attribution percentages must sum to 100%
- Income sources have proper tax categorization
- Bank import batches maintain referential integrity
- Reconciliation status must be consistent across related transactions
- Transaction dates must be chronologically valid

**Core Behaviors**:

- **Bank Statement Import**: Process CSV files with duplicate detection and batch tracking
- **Manual Transaction Entry**: Direct transaction creation with validation
- **AI-Powered Categorization**: Auto-suggest budget categories based on merchant, amount, and historical patterns
- **Smart Attribution**: AI-recommended personal vs rental splits and family member attribution
- **Transaction Reconciliation**: Match against budget realizations and planning items
- **Income Source Management**: Track employment, investment, and rental income sources
- **Duplicate Detection**: Prevent duplicate imports with fuzzy matching
- **Historical Analysis**: Generate spending patterns and trends for AI learning

**AI Integration**:

- `TransactionCategorizer`: AI-powered transaction categorization with learning from corrections
- `AttributionSuggester`: Smart personal vs rental and family member attribution suggestions  
- `ImportProcessor`: Processes CSV bank statements with intelligent parsing and validation
- `DuplicateDetector`: Identifies potential duplicate transactions across imports and manual entries
- `ReconciliationMatcher`: Matches transactions against budget realizations and planning items
- `SpendingAnalyser`: Analyses transaction patterns for budget variance and optimization insights

**Integration Events Consumed**:

- `PlanningListRealized`: Creates expected transactions for reconciliation matching
- `BudgetRealizationCreated`: Links transactions to budget realizations for variance tracking

**Integration Events Published**:

- `TransactionImported`: Notifies other aggregates of new financial data
- `TransactionCategorized`: Updates budget aggregate with spending against categories
- `IncomeReceived`: Notifies budget aggregate of actual income vs projected

**Key Workflows**:

1. **Bank Statement Import Process**:
   - Upload CSV file with batch tracking
   - Parse and validate transaction data
   - AI-powered duplicate detection against existing transactions
   - Auto-categorize using merchant patterns and historical data
   - Suggest attributions based on spending patterns
   - Queue for user review and confirmation

2. **Manual Transaction Entry**:
   - Direct transaction creation with category suggestion
   - Real-time attribution recommendations
   - Automatic budget category mapping
   - Integration with receipt scanning (future)

3. **Transaction Reconciliation**:
   - Match imported transactions against planned items
   - Reconcile against budget realizations
   - Identify unplanned spending for budget adjustment suggestions
   - Track spending variance against allocations

4. **Income Source Management**:
   - Track employment income with deductions (PAYE, UIF, SDL, Medical Aid)
   - Monitor investment returns (dividends, interest, capital gains)
   - Handle bonus tracking and salary increase modelling
   - Support rental income scenarios (actual and what-if)

5. **Attribution and Tax Reporting**:
   - Apply personal vs rental attribution rules consistently
   - Generate tax-ready reports for rental income and deductions
   - Support family member attribution for personal finance tracking
   - Export data for accounting software integration

6. **Financial Analysis and Insights**:
   - Spending pattern analysis for budget optimization
   - Cash flow projections based on historical data
   - Category variance reporting and trend analysis
   - ROI calculations for rental income scenarios

**South African Financial Context**:

- **Currency**: Primary ZAR (South African Rand) with multi-currency support
- **Tax Deductions**: Proper categorization for SARS reporting
- **Employment Income**: Handle PAYE, UIF, SDL, medical aid deductions
- **Banking Integration**: Support major SA bank CSV formats
- **Municipal Services**: Integration with utility billing and municipal account management

### Event Aggregate

**Root**: Event

**Entities**: RecurringEvent, EventSource

**Value Objects**: EventDate, EventType, Recurrence, BudgetReference, EventCreationSource

**Primary Focus**: **Time planning and calendar management** with optional budget reconciliation

**Invariants**:

- Events are primarily for scheduling and time management
- Budget references are optional and used only for reconciliation
- Recurring events maintain consistency with their patterns
- Event creation source must be tracked for audit purposes

**Event-Budget Relationship**:

- **Optional 1:1** relationship (not all events have budget impact)
- **Loose coupling** via `BudgetReference` value object
- **Application orchestration** handles budget modifications (not domain-level coupling)
- **Purpose**: Easier reconciliation when budget impact exists

**Event Creation Sources**:

- **Manual**: User creates events directly
- **PlanningList**: One event created per realized planning list
- **Maintenance**: Automatic maintenance events from appliance scheduling
- **Future**: Extensible pattern for additional automated event types

**Recurring Event Patterns**:

1. **Simple Recurring Events**: Monthly utility meter reading reminders (water, electricity)
   - Fixed schedule (e.g., 15th of each month)
   - Simple reminder to take reading - no complexity beyond basic notification

2. **AI-Driven Maintenance Scheduling**: Smart appliance maintenance considering:
   - **Family Schedule**: Avoid busy periods, school holidays, work commitments
   - **Cost Timing**: Align with budget cycles, avoid expensive periods
   - **Seasonal Patterns**: Air conditioning maintenance before summer, fireplace preparation before winter
   - **No Usage Intensity**: AI focuses on timing optimization, not usage-based triggers

**Maintenance Scheduling Split Responsibility**:

- **Appliance Domain**: Determines **what** maintenance is needed and **base frequency**
  - Service intervals (every 6 months for air-conditioner, yearly for appliances)
  - Seasonal requirements (AC before summer, heating before winter)
  - Criticality levels (urgent vs routine)
  - Publishes `ApplianceMaintenanceRequired` events

- **Event Domain**: Determines **when specifically** to schedule given constraints
  - Family schedule conflicts and availability
  - Budget timing coordination  
  - Multiple appliance coordination
  - Creates optimized calendar events

**AI Integration**:

- `SmartScheduler`: AI-powered conflict detection and time optimization (focused on time conflicts, not budget)
- `AutomatedEventCreator`: Creates events from integration events (maintenance, future automation types)
- `MaintenanceScheduler`: AI-driven maintenance **timing optimization** considering family schedule, cost timing, and seasonal patterns

**Integration Events Consumed**:

- `PlanningListRealized`: Creates **one event** per realized planning list (e.g., "Shopping Trip for Weekly Groceries")
- `ApplianceMaintenanceRequired`: Creates maintenance events with future extensibility for other automated types

**Domain Boundaries**:

- **Single Responsibility**: Event Aggregate manages time/calendar concerns only
- **Loose Coupling**: Optional budget references avoid tight coupling with Budget Aggregate
- **Integration via Events**: Communicates with other aggregates through domain events

## Integration Event Design

### Core Integration Events

**PlanningListRealized Event**:

```csharp
public record PlanningListRealized(
    PlanningListId ListId,
    IReadOnlyList<PlanningItem> Items,
    DateTime RealizedAt,
    PersonId RealizedBy
) : IIntegrationEvent;
```

**Household Domain Events**:

```csharp
public record ApplianceMaintenanceRequired(
    ApplianceId ApplianceId,
    MaintenanceType MaintenanceType,
    MaintenancePriority Priority,
    DateTime DueBy,
    DateTime PublishedAt
) : IIntegrationEvent;

public record UtilityUsageRecorded(
    UtilityAccountId AccountId,
    UtilityType UtilityType,
    UsageAmount Amount,
    RentalAttribution Attribution,
    DateOnly PeriodEnd,
    DateTime RecordedAt
) : IIntegrationEvent;
```

```csharp
public record TransactionImported(
    FinancialAccountId AccountId,
    IReadOnlyList<TransactionId> TransactionIds,
    ImportBatchId BatchId,
    DateTime ImportedAt
) : IIntegrationEvent;

public record TransactionCategorized(
    TransactionId TransactionId,
    BudgetCategoryId CategoryId,
    Attribution Attribution,
    DateTime CategorizedAt,
    PersonId CategorizedBy
) : IIntegrationEvent;

public record IncomeReceived(
    IncomeSourceId SourceId,
    Money Amount,
    DateOnly ReceivedDate,
    DateTime RecordedAt
) : IIntegrationEvent;

public record BudgetRealizationCreated(
    BudgetRealizationId RealizationId,
    BudgetCategoryId CategoryId,
    Money Amount,
    DateOnly PeriodDate,
    DateTime CreatedAt
) : IIntegrationEvent;
```

### Cross-Aggregate Processing

**Budget Aggregate Response**:

1. Receives polymorphic PlanningItems via pattern matching
2. Categorizes using `TransactionCategorizer` and historical patterns
3. Creates budget realizations in appropriate categories
4. Validates against allocations, provides warnings for over-budget
5. Handles specialized items (e.g., `PantryPlanningItem` → Groceries category)

**Event Aggregate Response**:

1. Receives polymorphic PlanningItems via pattern matching
2. Creates different event types based on planning item type:
   - `BasicPlanningItem` → Basic calendar events
   - `MaintenancePlanningItem` → Maintenance schedule events with appliance context
   - `PantryPlanningItem` → Shopping/meal events with urgency handling
3. Uses `SmartScheduler` for conflict detection and optimal timing

**Financial Aggregate Response**:

1. Receives polymorphic PlanningItems from `PlanningListRealized`
2. Creates expected transactions for future reconciliation matching
3. Applies type-specific attribution suggestions:
   - `PantryPlanningItem` → Family attribution by default
   - `MaintenancePlanningItem` → Shared attribution for appliances
4. Uses `AttributionSuggester` for preliminary attribution
5. Queues items for user confirmation when actual purchases occur

**Financial Integration Workflows**:

- **Transaction Import → Budget Reconciliation**: When `TransactionImported` is published, Budget Aggregate matches against existing realizations
- **Budget Realization → Financial Tracking**: When `BudgetRealizationCreated` is published, Financial Aggregate prepares for reconciliation
- **Income Tracking**: `IncomeReceived` events update Budget Aggregate's actual vs projected income tracking
- **Category Spending Updates**: `TransactionCategorized` events provide real-time budget variance updates

## Domain Services

### Attribution Calculator (Shared)

**Purpose**: Centralized business rules for personal vs rental attribution

**Interface**:

```csharp
public class AttributionCalculator 
{
    public Attribution CalculateForTransaction(Transaction transaction, HouseholdConfig config);
    public Attribution CalculateForUtilityUsage(UtilityUsage usage, HouseholdConfig config);
    public Attribution CalculateForPlanningItem(PlanningItem item, HouseholdConfig config);
}
```

**Usage**: Used by Budget, Financial, and Utility aggregates for consistent attribution logic

## Template Learning Strategy

**Automatic Pattern Detection**:

- No manual template association required
- AI detects recurring patterns across multiple lists
- Templates created automatically when 3+ similar lists detected
- Template suggestions offered but not mandatory

**Pattern Recognition Factors**:

- Similar item descriptions and groupings
- Recurring time patterns (weekly groceries, monthly supplies)
- Budget category patterns
- Family member attribution patterns

## AI Service Interfaces (Purpose-Driven Naming)

Following the "Purpose-Driven Naming" principle, all AI services are named for their capabilities:

### Planning & Inventory Domain

- `ListPhotoInterpreter`: Handwritten list photo → digital ListItems
- `BudgetImpactAnalyser`: Analyses planning list impact on budget
- `PantryOptimizer`: Suggests meals based on current inventory and expiration dates
- `ShoppingListGenerator`: Creates shopping lists from meal plans and pantry gaps
- `ExpirationTracker`: Suggests meals to use expiring ingredients
- `RecipeRecommender`: Suggests recipes based on family preferences and pantry stock

### Budget Domain  

- `BudgetAnalyser`: Spending analysis and variance detection
- `TransactionCategorizer`: Maps items/transactions to budget categories
- `RentalCostAnalyser`: Extracts rental-attributable costs for tax reporting

### Event Domain

- `SmartScheduler`: Conflict detection and budget-aware scheduling

### Household Domain

- `ApplianceMaintenanceRequirements`: Determines what maintenance is needed and base frequency for configured appliances
- `UtilityOptimizer`: Efficiency tracking and optimization recommendations based on SA utility patterns
- `RentalAttributionAnalyser`: Analyses bill changes to suggest rental vs house attribution percentages

### Financial Domain

- `TransactionCategorizer`: AI-powered transaction categorization with learning from corrections
- `AttributionSuggester`: Smart personal vs rental and family member attribution suggestions  
- `ImportProcessor`: Processes CSV bank statements with intelligent parsing and validation
- `DuplicateDetector`: Identifies potential duplicate transactions across imports and manual entries
- `ReconciliationMatcher`: Matches transactions against budget realizations and planning items
- `SpendingAnalyser`: Analyses transaction patterns for budget variance and optimization insights

## Key Design Principles Applied

1. [**"Blueberry Muffin" AI Integration**](../explanations/concepts-and-terminology.md#the-blueberry-muffin-approach-to-ai-integration)
2. [**"Rich Domain Model with Agentic AI"**](../explanations/concepts-and-terminology.md#rich-domain-model-with-agentic-ai)
3. [**"Purpose-Driven Naming"**](../explanations/concepts-and-terminology.md#purpose-driven-naming)
4. [**"Natural Workflow Preservation"**](../explanations/concepts-and-terminology.md#natural-workflow-preservation)
5. **Eventual Consistency**: Cross-aggregate communication via integration events
6. **Single Responsibility**: Each aggregate owns its domain logic and AI integration
