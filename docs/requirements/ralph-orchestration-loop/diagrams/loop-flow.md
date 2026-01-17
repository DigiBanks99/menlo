# Ralph Orchestration Loop - Flow Diagram

## High-Level Architecture

```mermaid
flowchart TD
    subgraph Host["Host System (Windows + WSL2)"]
        subgraph DC["Devcontainer (Podman)"]
            RALPH[ralph.sh]
            CC[Claude Code CLI]
            TOOLS[".NET 10 | Node 24 | Aspire CLI"]
        end
        PODMAN[Podman Engine]
    end

    RALPH --> CC
    CC --> TOOLS
    DC --> PODMAN
```

## Planning Loop Flow

```mermaid
flowchart TD
    START([ralph.sh plan]) --> LOAD[Load PROMPT_PLAN.md]
    LOAD --> READ_SPECS[Read /docs/requirements/*<br/>READ-ONLY]
    READ_SPECS --> READ_AGENT[Read AGENT.md]
    READ_AGENT --> SPAWN[Spawn up to 5 subagents]

    SPAWN --> S1[Search src/api/]
    SPAWN --> S2[Search src/lib/]
    SPAWN --> S3[Search src/ui/web/]
    SPAWN --> S4[Search for TODO/FIXME]
    SPAWN --> S5[Compare specs vs code]

    S1 --> COLLECT[Collect findings]
    S2 --> COLLECT
    S3 --> COLLECT
    S4 --> COLLECT
    S5 --> COLLECT

    COLLECT --> WRITE[Write /docs/plans/fix_plan.md]
    WRITE --> DONE([Planning Complete])
```

## Building Loop Flow

```mermaid
flowchart TD
    START([ralph.sh build]) --> LOAD[Load PROMPT_BUILD.md]
    LOAD --> READ[Read specs, fix_plan.md, AGENT.md]
    READ --> CHOOSE[Choose most important item]

    CHOOSE --> SEARCH[Search codebase<br/>up to 5 subagents]
    SEARCH --> IMPL[Implement changes<br/>up to 5 subagents]
    IMPL --> VAL{Validation<br/>1 subagent}

    VAL --> ASPIRE[aspire run<br/>all healthy?]
    ASPIRE --> |No| BUG[Document bug in fix_plan.md]
    BUG --> FIX[Spawn subagent to fix]
    FIX --> VAL

    ASPIRE --> |Yes| BUILD[dotnet build/test]
    BUILD --> |Fail| BUG
    BUILD --> |Pass| PNPM[pnpm test/lint]
    PNPM --> |Fail| BUG
    PNPM --> |Pass| UPDATE[Update fix_plan.md<br/>Update AGENT.md]

    UPDATE --> GIT[git commit]
    GIT --> PR{Significant<br/>change?}
    PR --> |Yes| GHPR[gh pr create]
    PR --> |No| SLEEP
    GHPR --> SLEEP[Sleep 5s]
    SLEEP --> LOAD
```

## Validation Back Pressure

```mermaid
flowchart LR
    subgraph BackPressure["Validation Pipeline (Single Subagent)"]
        A1[aspire run] --> |healthy| A2[dotnet build]
        A2 --> |pass| A3[dotnet test]
        A3 --> |pass| A4[pnpm test:all]
        A4 --> |pass| A5[pnpm lint]
        A5 --> |pass| COMMIT[Safe to Commit]

        A1 --> |unhealthy| FAIL[Document & Fix]
        A2 --> |fail| FAIL
        A3 --> |fail| FAIL
        A4 --> |fail| FAIL
        A5 --> |fail| FAIL
    end
```

## Bug Handling Workflow

```mermaid
sequenceDiagram
    autonumber
    participant R as Ralph (main)
    participant FP as fix_plan.md
    participant S as Subagent

    R->>R: Encounter bug/issue
    R->>FP: Document bug FIRST
    Note over FP: - [ ] Bug: description...
    R->>S: Spawn subagent to fix
    S->>S: Attempt fix
    alt Fix successful
        S->>R: Report success
        R->>FP: Mark complete
    else Fix failed
        S->>R: Report failure
        R->>FP: Update with findings
        Note over R: Next loop will retry
    end
```

## Subagent Limits (Claude Pro)

```mermaid
pie title Subagent Budget Per Loop
    "Search/Write (max 5)" : 5
    "Validation (exactly 1)" : 1
    "Reserved for fixes" : 2
```

## File Relationships

```mermaid
graph LR
    subgraph ReadOnly["Read-Only (Specs)"]
        SPECS["/docs/requirements/*"]
    end

    subgraph ReadWrite["Read-Write"]
        AGENT["/AGENT.md"]
        PLAN["/docs/plans/fix_plan.md"]
        CODE["src/**/*"]
    end

    subgraph Prompts["Loop Instructions"]
        PP[PROMPT_PLAN.md]
        PB[PROMPT_BUILD.md]
    end

    PP --> |reads| SPECS
    PP --> |reads| AGENT
    PP --> |writes| PLAN

    PB --> |reads| SPECS
    PB --> |reads| PLAN
    PB --> |reads/writes| AGENT
    PB --> |writes| CODE
    PB --> |updates| PLAN
```