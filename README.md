# Menlo Home Management Application

An AI-enhanced family home management application designed for a South African family of 5, focusing on budget management, planning coordination, and rental income analysis.

## 🎯 Core Philosophy

This application follows a **"Blueberry Muffin" approach** to AI integration - where AI agents are embedded seamlessly throughout existing workflows rather than existing as separate features.
The goal is to preserve natural family workflows (especially handwritten planning lists) while adding intelligent automation behind the scenes.

## 🏗️ Architecture

The architecture is document in full in the [Architecture Document](docs/explanations/architecture-document.md).

In summary it is a **Hybrid Cloud-Local Design** optimized for cost-conscious experimentation:

- **Frontend**: Angular PWA on Azure Static Web Apps (Free tier)
- **Backend**: .NET Core API on home server via Cloudflare Tunnel
- **Database**: PostgreSQL (local)
- **AI**: Local Ollama with Microsoft Phi models (privacy-first)
- **Model**: Vertical slice architecture with a rich Domain Model

## 🤖 AI Integration Approach

- **Embedded Intelligence**: AI woven into features, not standalone tools
- **Natural Workflow Preservation**: Enhances existing habits rather than replacing them
- **Privacy-First**: All AI processing happens locally
- **Correction-Based Learning**: Improves through user feedback

## 👥 Family-Centric Design

Built around the **CFO-COO Dynamic**:

- **CFO Role (Husband)**: Strategic financial analysis, budget variance, rental ROI
- **COO Role (Wife)**: Operational planning, scheduling, resource coordination

## 📚 Documentation

### Essential Reads

- **[Business Requirements](docs/requirements/business-requirements.md)** - Complete feature specifications
- **[Concepts & Terminology Guide](docs/explanations/concepts-and-terminology.md)** - Core philosophy and design patterns
- **[Architecture Decision Record](docs/decisions/adr-001-hosting-strategy.md)** - Hosting strategy analysis
- **[C4 Diagrams](docs/diagrams)** - System architecture visualization

### Quick Start

Ready to begin development? Follow these paths based on your focus:

#### **🚀 Development Roadmaps**

- **[Implementation Roadmap](docs/requirements/implementation-roadmap.md)** - High-level strategic approach and family validation criteria

#### **🔧 Development Environment**

- Windows 10 development machine with Docker Desktop
- .NET 9.0 with clean architecture patterns  
- Docker stack: PostgreSQL + Ollama for local AI
- Test-driven development: Unit → Integration → API testing

## 🛠️ Technology Stack

- **Frontend**: Angular 20, TypeScript, PWA capabilities
- **Backend**: .NET Core, Entity Framework Core
- **Database**: PostgreSQL
- **AI Framework**: Microsoft Semantic Kernel + Ollama (Phi models)
- **Hosting**: Azure Static Web Apps + Cloudflare Tunnel
- **Infrastructure**: Home server with UPS for load shedding resilience

## 🔑 Key Features

- **Handwritten List Interpretation**: Photo capture → AI analysis → budget impact
- **Smart Transaction Categorization**: AI learns from corrections
- **Budget-Aware Scheduling**: Event planning considers financial implications
- **Rental Income Analysis**: AI-enhanced ROI calculations (on-request)
- **Family Coordination**: Cross-domain contextual intelligence
