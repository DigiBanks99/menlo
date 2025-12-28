# Home Management Application - Domain Model Diagram

This diagram shows the detailed domain model of the Menlo Home Management Application, illustrating the entities, value objects, domain services, and their relationships within each bounded context.

```mermaid
graph TB
    %% Shared Kernel
    subgraph SharedKernel["🔧 Shared Kernel"]
        Money["💵 Money<br/>Value Object<br/>Currency-safe amount<br/>with allocation logic"]
    end

    %% Planning & Inventory Bounded Context
    subgraph PlanningContext["📝 Planning & Inventory Bounded Context"]
        %% Planning Entities
        subgraph PlanningEntities["Planning Entities"]
            PlanningList[📋 PlanningList<br/>Aggregate Root<br/>Coordinates handwritten list<br/>workflow and AI analysis]
            ListItem[📝 ListItem<br/>Entity<br/>Individual items within<br/>a planning list]
            ListTemplate[📄 ListTemplate<br/>Entity<br/>Auto-detected patterns<br/>from recurring lists]
            PantryItem[🥫 PantryItem<br/>Entity<br/>Inventory tracking with<br/>stock levels and expiration dates]
            Recipe[🍽️ Recipe<br/>Entity<br/>Family recipes with<br/>ingredient requirements]
            MealPlan[📅 MealPlan<br/>Entity<br/>Weekly/monthly meal planning<br/>with automatic ingredient calculation]
        end
        
        %% Planning Item Hierarchy
        subgraph PlanningItemHierarchy["Planning Item Inheritance Hierarchy"]
            PlanningItem[📦 PlanningItem<br/>Abstract Record<br/>Base planning item<br/>clean and simple]
            BasicPlanningItem[📝 BasicPlanningItem<br/>Record<br/>Simple planning items<br/>without specialization]
            PantryPlanningItem[🥫 PantryPlanningItem<br/>Record<br/>Pantry-specific items with<br/>recipe and urgency context]
            MaintenancePlanningItem[🔧 MaintenancePlanningItem<br/>Record<br/>Maintenance items with<br/>appliance and priority context]
        end
        
        %% Planning Value Objects
        subgraph PlanningValueObjects["Planning Value Objects"]
            CostEstimate[💰 CostEstimate<br/>Value Object<br/>Money amount with<br/>confidence level]
            ListType[📂 ListType<br/>Value Object<br/>Shopping, meal planning,<br/>event planning]
            ListState[🔄 ListState<br/>Value Object<br/>Created → PhotoCaptured →<br/>AIProcessed → UserReviewed → Realized]
            Quantity[📏 Quantity<br/>Value Object<br/>Amount with units for<br/>pantry management]
            ProductId[🏷️ ProductId<br/>Value Object<br/>Product identification<br/>for inventory tracking]
            Ingredient[🥕 Ingredient<br/>Value Object<br/>Recipe ingredients with<br/>quantities and alternatives]
        end
        
        %% Planning AI Services
        subgraph PlanningAI["Planning AI Services"]
            ListPhotoInterpreter[📷 ListPhotoInterpreter<br/>Domain Service<br/>Handwritten list photo →<br/>appropriate planning item types]
            BudgetImpactAnalyserPlanning[💵 BudgetImpactAnalyser<br/>Domain Service<br/>Analyses planning list impact<br/>on budget all item types]
            PantryOptimizer[🥫 PantryOptimizer<br/>Domain Service<br/>Suggests meals and generates<br/>PantryPlanningItem based on inventory]
            ShoppingListGenerator[🛒 ShoppingListGenerator<br/>Domain Service<br/>Creates lists with PantryPlanningItem<br/>from meal plans and pantry gaps]
            SmartPlanningItemCreator[🧠 SmartPlanningItemCreator<br/>Domain Service<br/>Determines appropriate planning<br/>item type based on context]
        end
    end
    
    %% Budget Bounded Context
    subgraph BudgetContext["💰 Budget Bounded Context"]
        %% Budget Entities
        subgraph BudgetEntities["Budget Entities"]
            Budget[💰 Budget<br/>Aggregate Root<br/>Contains categories, allocations,<br/>realizations, and transactions]
            BudgetCategory[📂 BudgetCategory<br/>Entity<br/>Hierarchical budget categories<br/>with unlimited nesting]
            BudgetAllocation[📋 BudgetAllocation<br/>Entity<br/>Planned amounts at leaf<br/>categories based on experience]
            BudgetRealization[✅ BudgetRealization<br/>Entity<br/>Concrete budget items from<br/>manual planning list processing]
            BudgetTransaction[💳 BudgetTransaction<br/>Entity<br/>Actual spent amounts from<br/>bank statements/receipts]
        end
        
        %% Budget Value Objects
        subgraph BudgetValueObjects["Budget Value Objects"]
            Money[💵 Money<br/>Value Object<br/>ZAR amount with<br/>attribution rules]
            AttributionRule[🎯 AttributionRule<br/>Value Object<br/>Personal vs rental<br/>percentage splits for shared expenses]
            BudgetPeriod[📅 BudgetPeriod<br/>Value Object<br/>Monthly, quarterly,<br/>yearly periods]
            CategoryPath[🗂️ CategoryPath<br/>Value Object<br/>Hierarchical category<br/>navigation path]
        end
        
        %% Budget AI Services
        subgraph BudgetAI["Budget AI Services"]
            BudgetAnalyser[📊 BudgetAnalyser<br/>Domain Service<br/>AI-enhanced spending analysis<br/>and variance detection]
            TransactionCategorizerBudget[🏷️ TransactionCategorizer<br/>Domain Service<br/>Maps items/transactions<br/>to budget categories]
            RentalCostAnalyser[🏠 RentalCostAnalyser<br/>Domain Service<br/>Extracts rental-attributable<br/>costs for tax reporting]
        end
    end
    
    %% Financial Bounded Context
    subgraph FinancialContext["💳 Financial Bounded Context"]
        %% Financial Entities
        subgraph FinancialEntities["Financial Entities"]
            FinancialAccount[🏦 FinancialAccount<br/>Aggregate Root<br/>Bank accounts with transaction<br/>management and reconciliation]
            Transaction[💳 Transaction<br/>Entity<br/>Individual financial transactions<br/>with categorization and attribution]
            IncomeSource[💼 IncomeSource<br/>Entity<br/>Employment, investment,<br/>and rental income sources]
            BankImport[📥 BankImport<br/>Entity<br/>Bank statement import batches<br/>with duplicate detection]
            TransactionReconciliation[🔗 TransactionReconciliation<br/>Entity<br/>Links transactions to budget<br/>realizations and planning items]
        end
        
        %% Financial Value Objects
        subgraph FinancialValueObjects["Financial Value Objects"]
            TransactionCategory[🏷️ TransactionCategory<br/>Value Object<br/>Category mapping to<br/>budget hierarchy]
            Attribution[🎯 Attribution<br/>Value Object<br/>Personal vs rental and<br/>family member splits]
            ImportBatch[📦 ImportBatch<br/>Value Object<br/>Batch tracking for<br/>bank statement imports]
            ReconciliationStatus[✅ ReconciliationStatus<br/>Value Object<br/>Unreconciled, Matched,<br/>Confirmed, Disputed]
            TransactionType[🔄 TransactionType<br/>Value Object<br/>Debit, Credit, Transfer,<br/>Fee, Interest]
        end
        
        %% Financial AI Services
        subgraph FinancialAI["Financial AI Services"]
            ImportProcessor[📥 ImportProcessor<br/>Domain Service<br/>CSV bank statement processing<br/>with intelligent parsing and validation]
            TransactionCategorizerFinancial[🏷️ TransactionCategorizer<br/>Domain Service<br/>AI-powered transaction categorization<br/>with learning from corrections]
            AttributionSuggester[🎯 AttributionSuggester<br/>Domain Service<br/>Smart personal vs rental and<br/>family member attribution suggestions]
            DuplicateDetector[🔍 DuplicateDetector<br/>Domain Service<br/>Identifies potential duplicate<br/>transactions across imports and manual entries]
            ReconciliationMatcher[🔗 ReconciliationMatcher<br/>Domain Service<br/>Matches transactions against<br/>budget realizations and planning items]
            SpendingAnalyser[📊 SpendingAnalyser<br/>Domain Service<br/>Analyses transaction patterns for<br/>budget variance and optimization insights]
        end
    end
    
    %% Household Bounded Context
    subgraph HouseholdContext["🏡 Household Bounded Context"]
        %% Household Configuration Entities
        subgraph HouseholdConfigEntities["Household Configuration Entities"]
            Household[🏡 Household<br/>Aggregate Root<br/>Family configuration<br/>and appliance registry]
            Person[👤 Person<br/>Entity<br/>Family member for event<br/>attribution not utility usage]
            Appliance[🔌 Appliance<br/>Entity<br/>Registered appliances with<br/>optional maintenance configuration]
        end
        
        %% Utility Account Entities
        subgraph UtilityAccountEntities["Utility Account Entities"]
            UtilityAccount[⚡ UtilityAccount<br/>Aggregate Root<br/>SA-specific utility tracking<br/>with rental attribution]
            UtilityReading[📊 UtilityReading<br/>Entity<br/>Meter readings and<br/>usage calculations]
            UtilityInvoice[🧾 UtilityInvoice<br/>Entity<br/>Municipal invoices and<br/>gas refill tracking]
        end
        
        %% Household Value Objects
        subgraph HouseholdValueObjects["Household Value Objects"]
            PersonId[👤 PersonId<br/>Value Object<br/>Unique family member identifier<br/>for event attribution]
            ApplianceSpec[🔌 ApplianceSpec<br/>Value Object<br/>Make, model, efficiency ratings,<br/>warranty info]
            MaintenanceSchedule[📅 MaintenanceSchedule<br/>Value Object<br/>Optional configurable<br/>maintenance requirements]
            Reading[📊 Reading<br/>Value Object<br/>Meter reading with<br/>usage calculations]
            UsageCalculation[📈 UsageCalculation<br/>Value Object<br/>Usage attribution and<br/>efficiency metrics]
            RentalAttribution[🏠 RentalAttribution<br/>Value Object<br/>House vs rental<br/>percentage splits]
        end
        
        %% Household AI Services
        subgraph HouseholdAI["Household AI Services"]
            ApplianceMaintenanceRequirements[🔧 ApplianceMaintenanceRequirements<br/>Domain Service<br/>Determines what maintenance is<br/>needed for configured appliances]
            UtilityOptimizer[⚡ UtilityOptimizer<br/>Domain Service<br/>Efficiency tracking and<br/>optimization for SA utilities]
            RentalAttributionAnalyser[🏠 RentalAttributionAnalyser<br/>Domain Service<br/>Analyses bill changes for<br/>rental vs house attribution]
        end
    end
    
    %% Event Bounded Context
    subgraph EventContext["📅 Event Bounded Context"]
        %% Event Entities
        subgraph EventEntities["Event Entities"]
            Event[📅 Event<br/>Aggregate Root<br/>Family calendar events<br/>focused on time planning]
            RecurringEvent[🔄 RecurringEvent<br/>Entity<br/>Events that repeat<br/>with patterns]
            EventSource[📝 EventSource<br/>Entity<br/>Tracks how event was created<br/>manual, planning list, maintenance, etc]
        end
        
        %% Event Value Objects
        subgraph EventValueObjects["Event Value Objects"]
            EventDate[📅 EventDate<br/>Value Object<br/>Date/time with<br/>timezone handling]
            EventType[📂 EventType<br/>Value Object<br/>Shopping, celebration,<br/>maintenance, etc]
            Recurrence[🔄 Recurrence<br/>Value Object<br/>Daily, weekly,<br/>monthly patterns]
            BudgetReference[💰 BudgetReference<br/>Value Object<br/>Optional reference to<br/>related budget realization]
            EventCreationSource[📝 EventCreationSource<br/>Value Object<br/>Manual, PlanningList, Maintenance,<br/>Future automation types]
        end
        
        %% Event AI Services
        subgraph EventAI["Event AI Services"]
            SmartScheduler[🤖 SmartScheduler<br/>Domain Service<br/>AI-powered conflict detection<br/>and time optimization]
            AutomatedEventCreator[🤖 AutomatedEventCreator<br/>Domain Service<br/>Creates events from integration<br/>events maintenance, future types]
            MaintenanceScheduler[🔧 MaintenanceScheduler<br/>Domain Service<br/>AI-driven maintenance timing<br/>optimization considering constraints]
        end
    end
    
    %% Shared Services
    subgraph SharedServices["🔧 Shared Domain Services"]
        AttributionCalculator[🎯 AttributionCalculator<br/>Domain Service<br/>Centralised attribution logic<br/>for all aggregates]
    end
    
    %% Integration Events
    subgraph IntegrationEvents["📡 Integration Events"]
        PlanningListCreated[📝 PlanningListCreated<br/>Integration Event<br/>Published when planning list<br/>is created for analysis]
        PlanningListRealized[✅ PlanningListRealized<br/>Integration Event<br/>Published when planning list<br/>is realized with polymorphic PlanningItems]
        ApplianceMaintenanceRequired[🔧 ApplianceMaintenanceRequired<br/>Integration Event<br/>Published when configured<br/>appliance needs maintenance]
        TransactionImported[📥 TransactionImported<br/>Integration Event<br/>Published when new financial<br/>transactions are imported]
        BudgetRealizationCreated[✅ BudgetRealizationCreated<br/>Integration Event<br/>Published when budget<br/>realizations are created]
    end
    
    %% Planning Item Inheritance
    BasicPlanningItem --> PlanningItem
    PantryPlanningItem --> PlanningItem
    MaintenancePlanningItem --> PlanningItem
    
    %% Aggregate Internal Relationships - Planning
    PlanningList --> ListItem
    PlanningList --> ListTemplate
    PlanningList --> PantryItem
    PlanningList --> Recipe
    PlanningList --> MealPlan
    PlanningList --> ListPhotoInterpreter
    PlanningList --> BudgetImpactAnalyserPlanning
    PlanningList --> PantryOptimizer
    PlanningList --> ShoppingListGenerator
    PlanningList --> SmartPlanningItemCreator
    PantryPlanningItem --> Recipe
    MaintenancePlanningItem --> Appliance
    
    %% Aggregate Internal Relationships - Budget
    Budget --> BudgetCategory
    Budget --> BudgetAllocation
    Budget --> BudgetRealization
    Budget --> BudgetTransaction
    Budget --> BudgetAnalyser
    Budget --> TransactionCategorizerBudget
    Budget --> RentalCostAnalyser
    
    %% Aggregate Internal Relationships - Financial
    FinancialAccount --> Transaction
    FinancialAccount --> IncomeSource
    FinancialAccount --> BankImport
    FinancialAccount --> TransactionReconciliation
    FinancialAccount --> ImportProcessor
    FinancialAccount --> TransactionCategorizerFinancial
    FinancialAccount --> AttributionSuggester
    FinancialAccount --> DuplicateDetector
    FinancialAccount --> ReconciliationMatcher
    FinancialAccount --> SpendingAnalyser
    
    %% Aggregate Internal Relationships - Household
    Household --> Person
    Household --> Appliance
    Appliance --> MaintenanceSchedule
    Appliance --> ApplianceMaintenanceRequirements
    UtilityAccount --> UtilityReading
    UtilityAccount --> UtilityInvoice
    UtilityAccount --> UtilityOptimizer
    UtilityAccount --> RentalAttributionAnalyser
    
    %% Aggregate Internal Relationships - Event
    Event --> RecurringEvent
    Event --> EventSource
    Event --> SmartScheduler
    Event --> AutomatedEventCreator
    Event --> MaintenanceScheduler
    
    %% Value Object Usage (key relationships)
    ListItem --> CostEstimate
    PlanningList --> ListState
    PlanningList --> ListType
    BudgetCategory --> Money
    BudgetRealization --> AttributionRule
    BudgetCategory --> CategoryPath
    BudgetAllocation --> BudgetPeriod
    Event --> EventDate
    Event --> EventType
    Event --> BudgetReference
    RecurringEvent --> Recurrence
    Transaction --> Attribution
    Transaction --> TransactionCategory
    Transaction --> TransactionType
    BankImport --> ImportBatch
    TransactionReconciliation --> ReconciliationStatus
    Appliance --> ApplianceSpec
    Appliance --> MaintenanceSchedule
    Person --> PersonId
    UtilityReading --> Reading
    UtilityAccount --> RentalAttribution
    UtilityReading --> UsageCalculation
    
    %% Integration Events
    PlanningList --> PlanningListCreated
    PlanningList --> PlanningListRealized
    UtilityAccount --> ApplianceMaintenanceRequired
    FinancialAccount --> TransactionImported
    Budget --> BudgetRealizationCreated
    
    %% Cross-Aggregate via Events
    PlanningListCreated --> Budget
    PlanningListRealized --> Budget
    PlanningListRealized --> Event
    PlanningListRealized --> FinancialAccount
    BudgetRealizationCreated --> FinancialAccount
    TransactionImported --> Budget
    ApplianceMaintenanceRequired --> Event
    
    %% Shared Services Usage
    Budget --> AttributionCalculator
    FinancialAccount --> AttributionCalculator
    UtilityAccount --> AttributionCalculator
    TransactionCategorizerBudget --> AttributionCalculator
    TransactionCategorizerFinancial --> AttributionCalculator
    AttributionSuggester --> AttributionCalculator
    
    %% Styling
    classDef planningClass fill:#fff8e1,stroke:#f9a825,stroke-width:2px,color:#000
    classDef budgetClass fill:#e8f5e8,stroke:#388e3c,stroke-width:2px,color:#000
    classDef financialClass fill:#fce4ec,stroke:#c2185b,stroke-width:2px,color:#000
    classDef householdClass fill:#e0f2f1,stroke:#00695c,stroke-width:2px,color:#000
    classDef eventClass fill:#e1f5fe,stroke:#0277bd,stroke-width:2px,color:#000
    classDef sharedClass fill:#fafafa,stroke:#424242,stroke-width:2px,color:#000
    classDef integrationClass fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px,color:#000
    classDef hierarchyClass fill:#ffebee,stroke:#d32f2f,stroke-width:2px,color:#000
    
    %% Apply styles to planning context
    class PlanningList,ListItem,ListTemplate,PantryItem,Recipe,MealPlan planningClass
    class CostEstimate,ListType,ListState,Quantity,ProductId,Ingredient planningClass
    class ListPhotoInterpreter,BudgetImpactAnalyserPlanning,PantryOptimizer,ShoppingListGenerator,SmartPlanningItemCreator planningClass
    
    %% Apply styles to planning hierarchy
    class PlanningItem,BasicPlanningItem,PantryPlanningItem,MaintenancePlanningItem hierarchyClass
    
    %% Apply styles to budget context
    class Budget,BudgetCategory,BudgetAllocation,BudgetRealization,BudgetTransaction budgetClass
    class Money,AttributionRule,BudgetPeriod,CategoryPath budgetClass
    class BudgetAnalyser,TransactionCategorizerBudget,RentalCostAnalyser budgetClass
    
    %% Apply styles to financial context
    class FinancialAccount,Transaction,IncomeSource,BankImport,TransactionReconciliation financialClass
    class TransactionCategory,Attribution,ImportBatch,ReconciliationStatus,TransactionType financialClass
    class ImportProcessor,TransactionCategorizerFinancial,AttributionSuggester,DuplicateDetector,ReconciliationMatcher,SpendingAnalyser financialClass
    
    %% Apply styles to household context
    class Household,Person,Appliance,UtilityAccount,UtilityReading,UtilityInvoice householdClass
    class PersonId,ApplianceSpec,MaintenanceSchedule,Reading,UsageCalculation,RentalAttribution householdClass
    class ApplianceMaintenanceRequirements,UtilityOptimizer,RentalAttributionAnalyser householdClass
    
    %% Apply styles to event context
    class Event,RecurringEvent,EventSource eventClass
    class EventDate,EventType,Recurrence,BudgetReference,EventCreationSource eventClass
    class SmartScheduler,AutomatedEventCreator,MaintenanceScheduler eventClass
    
    %% Apply styles to shared and integration
    class AttributionCalculator sharedClass
    class Money sharedClass
    class PlanningListCreated,PlanningListRealized,ApplianceMaintenanceRequired,TransactionImported,BudgetRealizationCreated integrationClass
```

## Domain Model Highlights

### Planning & Inventory Bounded Context

**Core Philosophy**: Inheritance-based planning items with pantry management and clean separation

- **Planning Item Hierarchy**: Abstract base with Basic, Pantry, and Maintenance specializations
- **Pantry Management**: Dedicated entities for inventory, recipes, and meal planning
- **AI Services**: Handwriting recognition, budget impact analysis, meal optimization
- **State Management**: Created → PhotoCaptured → AIProcessed → UserReviewed → Realized

### Budget Bounded Context

**Core Philosophy**: Planned → Realized → Spent states with unified income/expense hierarchy

- **Hierarchical Categories**: Unlimited nesting with leaf-level planning
- **Attribution Rules**: Personal vs rental percentage splits for shared expenses
- **Manual Realization**: Budget realizations created from approved planning list suggestions
- **AI Analysis**: Variance detection and rental cost extraction

### Financial Bounded Context

**Core Philosophy**: Transaction management with AI-powered categorization and reconciliation

- **Import Processing**: CSV bank statements with duplicate detection and validation
- **Smart Categorization**: AI-powered with learning from user corrections
- **Attribution**: Personal vs rental and family member splits
- **Reconciliation**: Links transactions to budget realizations and planning items
- **SA Banking**: Support for major South African bank formats

### Household Bounded Context

**Core Philosophy**: Two aggregates - configuration and utilities with SA-specific tracking

#### Household Configuration Aggregate

- **Family Management**: Family members for event attribution (not utility usage)
- **Appliance Registry**: Optional maintenance configuration per appliance
- **Maintenance Scheduling**: Configurable per appliance, not mandatory

#### Utility Account Aggregate

- **SA Utilities**: Prepaid electricity, monthly water, municipal invoices, gas refills
- **Rental Attribution**: House vs rental percentages via retrospective analysis
- **Usage Optimization**: Efficiency tracking and recommendations

### Event Bounded Context

**Core Philosophy**: Time planning with optional budget references and split maintenance scheduling

- **Event Creation**: Manual, from planning lists, maintenance requirements
- **Smart Scheduling**: AI conflict detection with budget awareness
- **Budget Integration**: Optional references to budget realizations
- **Maintenance Split**: Appliances determine what/when (base frequency), Events determine when specifically (constraint optimization)

### Integration & Communication

#### Integration Events

- **Eventual Consistency**: Cross-aggregate communication via events only
- **Polymorphic Planning**: Events include planning item type information
- **Non-blocking Analysis**: Planning list creation triggers background budget analysis

#### Shared Services

- **Attribution Calculator**: Centralized logic for all attribution calculations
- **Domain Service**: Used across aggregates for consistent attribution rules

### Key Design Decisions

1. **Planning Item Inheritance**: Clean type-based specialization without context pollution
2. **Split Maintenance**: Configuration in Household, scheduling optimization in Events
3. **Manual Budget Realization**: Budget modifications require explicit user approval
4. **Attribution-First**: All financial data includes attribution from creation
5. **Privacy-First AI**: All domain services designed for local AI processing
6. **SA Context**: Municipal services, banking, and tax requirements built-in
