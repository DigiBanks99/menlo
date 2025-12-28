# Home Management Application - Business Requirements

## Table of Contents

- [Home Management Application - Business Requirements](#home-management-application---business-requirements)
  - [Table of Contents](#table-of-contents)
  - [Project Overview](#project-overview)
    - [Purpose](#purpose)
    - [Context](#context)
    - [Primary Users](#primary-users)
  - [Core Functional Requirements](#core-functional-requirements)
    - [Budget Management](#budget-management)
      - [Hierarchical Budget Structure](#hierarchical-budget-structure)
      - [Budget States and Allocation](#budget-states-and-allocation)
      - [Attribution and Splitting](#attribution-and-splitting)
    - [Income Tracking](#income-tracking)
      - [Employment Income](#employment-income)
      - [Investment and Other Income](#investment-and-other-income)
    - [Planning Lists and Templates](#planning-lists-and-templates)
      - [List Management](#list-management)
      - [Budget Integration](#budget-integration)
    - [Event and Calendar Integration](#event-and-calendar-integration)
      - [Event Types](#event-types)
      - [Internal Scheduling Capabilities](#internal-scheduling-capabilities)
      - [Budget Event Relationships](#budget-event-relationships)
    - [Transaction Management](#transaction-management)
      - [Transaction Recording](#transaction-recording)
      - [Transaction Attribution](#transaction-attribution)
    - [Utility and Appliance Management](#utility-and-appliance-management)
      - [Utility Tracking](#utility-tracking)
      - [Appliance Management](#appliance-management)
    - [Rental Income Analysis (On-Request Only)](#rental-income-analysis-on-request-only)
      - [What-if Scenarios](#what-if-scenarios)
      - [Decision Support](#decision-support)
  - [User Experience Requirements](#user-experience-requirements)
    - [Primary User Workflows](#primary-user-workflows)
    - [Interface Requirements](#interface-requirements)
    - [Notification System](#notification-system)
  - [Technical Requirements](#technical-requirements)
    - [Platform and Technology](#platform-and-technology)
    - [AI Integration Architecture](#ai-integration-architecture)
    - [Hosting Architecture](#hosting-architecture)
    - [Integration Capabilities](#integration-capabilities)
    - [Performance Requirements](#performance-requirements)
  - [Data and Privacy Requirements](#data-and-privacy-requirements)
    - [Data Management](#data-management)
    - [Backup and Recovery](#backup-and-recovery)
  - [Future Enhancement Opportunities](#future-enhancement-opportunities)
    - [AI and Automation](#ai-and-automation)
    - [Advanced Features](#advanced-features)
  - [Success Criteria](#success-criteria)
    - [Primary Goals](#primary-goals)
    - [Key Metrics](#key-metrics)
  - [Constraints and Assumptions](#constraints-and-assumptions)
    - [Constraints](#constraints)
    - [Assumptions](#assumptions)

## Project Overview

### Purpose

Develop a comprehensive home management application for a family of 5 (2 adults, 3 children) that preserves natural planning workflows while intelligently interpreting them into budget impacts,
coordinated schedules, and informed financial decisions, particularly regarding potential rental income from their new home.
The application acts as an AI interpreter that transforms handwritten planning lists into organized family management without disrupting existing thinking processes.

### Context

- Recently moved to a new full-title home in Tshwane, South Africa
- Need to establish expense tracking patterns for the new house
- Considering renting part of the house to students (tax and utility implications)
- Complex family scheduling with young children (one in Grade 1)
- Two working parents with coordinated schedules

### Primary Users

- **Husband**: Budget management, financial analysis, rental decision modelling (CFO persona)
- **Wife**: Planning lists, event coordination, family scheduling (COO persona)

## Core Functional Requirements

### Budget Management

#### Hierarchical Budget Structure

- Support unlimited nesting levels (composite pattern)
- Categories include:
  - Housing (Bond, Municipal Services, Security, Services, Maintenance, Utilities)
  - Transport (Petrol, Car Insurance, Maintenance, Licensing)
  - Family Living (Groceries, Clothing by person, Entertainment, Personal Care)
  - Education (School expenses by child, Activities by child)
  - Faith & Giving (Tithing, Donations)
  - Pets (Food, Veterinary, Insurance, Supplies)
  - Holidays & Travel (with detailed breakdown capability)

#### Budget States and Allocation

- **Abstract Allocation**: High-level budget amounts without detail
- **AI-Enhanced Realization Process**: Convert abstract budgets into detailed line items with full AI suggestions based on planning lists and historical data
- **Smart Budget Suggestions**: AI analyses completed planning lists, historical spending patterns, and seasonal trends to suggest comprehensive budget realizations
- Budget states: Allocated → Planning → Realized → Spent
- **Proactive Budget Adjustments**: AI suggests budget reallocations based on planning list analysis and spending trends
- Notification alerts when approaching budget thresholds

#### Attribution and Splitting

- Personal vs Rental expense attribution
- Percentage-based splitting for utilities and maintenance
- Tax deductible expense categorization
- Individual attribution for family members

### Financial Integrity

#### Money Handling

- **Precision**: All financial calculations must maintain high precision to avoid rounding errors.
- **Currency Safety**: The system must prevent accidental operations between different currencies (though ZAR is primary).
- **Allocation Accuracy**: Splitting amounts (e.g., monthly budgets) must preserve the total amount without losing cents (Penny Allocation).
- **Immutability**: Financial values must be immutable to ensure data integrity.

### Income Tracking

#### Employment Income

- Gross salary tracking per person
- South African deductions (PAYE, UIF, SDL, Medical Aid)
- Bonus tracking and modelling
- Salary increase calculations (twice yearly)

#### Investment and Other Income

- Investment returns (dividends, interest, capital gains)
- Rental income (actual and what-if scenarios)
- Savings growth tracking
- Rewards program estimates

### Planning Lists and Templates

#### List Management

- **Handwritten List Capture**: Photo capture of handwritten lists (MVP), future digital tablet integration
- **AI List Interpretation**: AI processes completed lists to extract items, costs, and planning intent
- **Contextual Assistance**: Non-intrusive helper panel suggests related items or patterns without disrupting workflow
- Weekly meal planning lists with suggestions for enjoyed but unmade meals
- Grocery lists generated from meal plans with cost predictions
- Event planning lists (birthdays, holidays, clothing needs) with contextual reminders
- Template lists that evolve from recurring patterns in captured lists

#### Budget Integration

- **Post-List Processing**: AI analyses completed lists for budget impact (no real-time interruption)
- **Intelligent Cost Estimation**: AI predicts costs based on historical data and current patterns
- **Automatic Budget Category Mapping**: AI maps list items to budget categories with user correction capability

### Event and Calendar Integration

#### Event Types

- Recurring events (weekly meal prep, monthly bills)
- One-time events (birthdays, school trips, holidays)
- Deadline-driven events with financial implications
- **AI-Suggested Events**: Events derived from planning list patterns and historical data

#### Internal Scheduling Capabilities

- **Family Calendar Management**: Centralized calendar for family activities and commitments
- **Smart Scheduling**: AI suggests optimal timing for activities based on family patterns
- **Conflict Detection**: Automatic detection of scheduling conflicts with smart resolution suggestions
- **Budget-Aware Scheduling**: Events have loose coupling to budgets to track planned and real expenses for events for events that have a financial impact

#### Budget Event Relationships

- Events can trigger budget realizations
- Calendar events linked to expense categories
- Notification system for upcoming expenses

### Transaction Management

#### Transaction Recording

- Manual transaction entry
- Bank statement import (CSV format initially)
- Receipt OCR processing (future enhancement)
- **AI-Powered Categorization**: AI suggests transaction categories based on merchant, amount, and historical patterns with user correction capability
- **Smart Attribution Suggestions**: AI recommends rental vs personal splits and family member attribution based on purchase patterns

#### Transaction Attribution

- Category mapping to budget hierarchy with AI assistance
- Rental vs personal expense splitting with intelligent suggestions
- Individual family member attribution based on historical patterns
- **User Correction Learning**: AI learns from user corrections to improve future suggestions

### Utility and Appliance Management

#### Utility Tracking

- Manual meter readings (electricity, water)
- Usage calculations between readings
- Cost attribution for rental scenarios
- Municipal services tracking (rates, levies, refuse)

#### Appliance Management

- Appliance inventory (make, model, purchase date, warranty)
- Usage estimation and efficiency tracking
- Maintenance scheduling based on usage patterns
- Replacement alerts based on efficiency thresholds
- Key appliances: Washing machine, Oven, Dishwasher

### Rental Income Analysis (On-Request Only)

#### What-if Scenarios

- **On-Demand Analysis**: Rental analysis triggered only when specifically requested by user
- Gross vs net rental income calculations
- Utility usage impact modelling with AI-enhanced predictions
- Tax implication calculations based on current financial data
- Maintenance cost attribution using historical patterns

#### Decision Support

- **AI-Enhanced ROI Calculations**: Comprehensive ROI analysis incorporating market trends and personal financial data
- Break-even analysis with sensitivity modelling
- Risk assessment tools with scenario planning
- **Smart Recommendation Engine**: AI provides rental strategy recommendations based on family financial goals

## User Experience Requirements

### Primary User Workflows

- **Wife's Planning Workflow**: Create lists → See budget impact → Coordinate events
- **Husband's Analysis Workflow**: Review spending → Analyze trends → Model scenarios

### Interface Requirements

- Cross-platform access (web-based PWA preferred)
- Mobile-responsive design
- Real-time updates when multiple users active
- Offline capability with sync when connected

### Notification System

- Budget threshold alerts
- Upcoming expense reminders
- Maintenance scheduling notifications
- Reallocation suggestions

## Technical Requirements

### Platform and Technology

- **Frontend**: Angular 21 Progressive Web App deployed on Cloudflare Pages (Free tier)
- **Backend**: .NET Core Web API running on home server accessed via Cloudflare Tunnel
- **Database**: PostgreSQL with Entity Framework Core
- **Hosting**: Hybrid architecture - Cloudflare Pages + Cloudflare Tunnel to home server
- **AI Framework**: Microsoft Semantic Kernel for AI orchestration
- **Local AI Models**: Ollama with Microsoft Phi models for cost-conscious local inference

### AI Integration Architecture

- **Cost-Conscious Local AI**: Ollama hosting Phi-4-mini and Phi-4-vision models locally
- **Semantic Kernel Integration**: AI agent orchestration and prompt management
- **Privacy-First Approach**: All AI processing happens locally, no data sent to external AI services
- **Non-Intrusive Design**: AI operates in background, presenting suggestions without interrupting workflows
- **Learning Capability**: AI improves suggestions based on user corrections and patterns

### Hosting Architecture

**Hybrid Cloud-Local Architecture:**

```text
[Family Devices] → [Cloudflare Pages Edge] → [Cloudflare Tunnel] → [Home Server API] → [Local AI + Database]
```

**Components:**

- **Frontend Hosting**: Cloudflare Pages (globally distributed edge delivery, free tier)
- **API Hosting**: Home server accessed via Cloudflare Tunnel (eliminates static IP requirement)
- **Database**: PostgreSQL on home server for full data control
- **AI Processing**: Local Ollama instance for privacy and cost savings
- **Connectivity**: Cloudflare Tunnel provides secure, encrypted connection without port forwarding

**Benefits:**

- Very low ongoing costs (~R165-365/month vs R1700-3200/month for full cloud)
- Complete AI privacy - no external AI service calls
- Fast UI delivery via Cloudflare global edge network
- Secure backend access without static IP
- Full control over family data

### Integration Capabilities

- Bank statement import (CSV initially)
- Calendar integration potential
- Future AI enhancement readiness
- Local data processing for privacy

### Performance Requirements

- Near real-time updates for budget changes
- Quick response times for list creation and editing
- Efficient reporting and analysis queries
- AI response times under 3 seconds for local processing

## Data and Privacy Requirements

### Data Management

- Local-first storage with cloud sync
- No reliance on browser storage APIs
- Secure financial data handling
- Family data privacy protection

### Backup and Recovery

- Regular data backups
- Export capabilities
- Data portability

## Future Enhancement Opportunities

### AI and Automation

- Local AI for transaction categorization
- Agentic AI for budget optimization suggestions
- Predictive modelling for expenses
- Smart reallocation recommendations

### Advanced Features

- Recipe and pantry management
- Smart home device integration
- Advanced reporting and analytics
- Multi-family/household support

## Success Criteria

### Primary Goals

- Accurate expense tracking and attribution for rental decision
- Improved family coordination and planning
- Reduced financial stress through better visibility
- Wife finds value and actively uses the planning features

### Key Metrics

- Monthly budget variance tracking
- Expense attribution accuracy for rental calculations
- User engagement (especially wife's list usage)
- Time saved on family coordination

## Constraints and Assumptions

### Constraints

- Cost-conscious hosting and infrastructure
- Single family usage (not multi-tenant)
- Manual data entry acceptable for MVP
- South African financial context (tax, terminology)

### Assumptions

- Users comfortable with web-based applications
- Steady internet connectivity for sync
- Willingness to manually enter initial data
- Family commitment to using the system consistently
