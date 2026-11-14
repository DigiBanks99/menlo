# Menlo Product Vision

This is what Menlo is for and what it does. It governs every Requirement: shape each one with this page in view, never on its own (see [Shaping a Requirement](../reference/work-items.md#shaping-a-requirement)).

## Purpose

Menlo helps a household plan and run its money and its time. It takes the way the household already plans (lists, a calendar, a yearly budget), turns that into a budget it can trust, compares the budget with what is actually spent, and supports bigger decisions like renting out part of the home.

Menlo doesn't change how the household plans. It works with the habits the household already has (see [Natural Workflow Preservation](concepts-and-terminology.md#natural-workflow-preservation)).

## Who it's for

Menlo is built for one household. Everyone who signs in belongs to that household and sees its data.

Two roles shape how people use it. One person can play both.

- **COO**: keeps the household's time and plans moving: lists, events and coordination.
- **CFO**: keeps an eye on the money and the bigger decisions: spending, trends and what-ifs.

Menlo brings the two views together, so decisions are informed by both sides (see [The CFO-COO Household Dynamic](concepts-and-terminology.md#the-cfo-coo-household-dynamic)).

## Features and capabilities

Menlo is made of **features** and **capabilities** (see [`GLOSSARY.md`](../../GLOSSARY.md)).

### Features

Features are the verticals: areas of the app with their own subject, each built end to end.

| Feature                 | What it covers                                                                                                                                                                                                         |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Budgeting and spend** | The household's money: the yearly budget and its changes, the spends recorded against it, each member's personal view and the household view, and any rental unit's income and costs.                                 |
| **Household calendar**  | The household's time: events, recurring commitments and clashes. Events that cost money tie into the budget.                                                                                                           |
| **Resources**           | What the household owns (the house, vehicles, appliances, devices: their cost, servicing, lifespan and replacement) and what it uses (electricity, water, bandwidth, measured over time), and the decisions about both. |

### Capabilities

Capabilities are the horizontals: things any feature can use.

| Capability        | What it does                                                                                                                                                                                                                                                                                                           |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **The agent**     | The household's PA. It keeps track of what the household misses because it is busy with more important things, and suggests improvements to how the household plans, tracks and acts. Where a feature allows it, it also acts. It serves the whole household, not one person. It augments the household; it is not a safety net. |
| **Lists**         | Captures plans the way they are already made, such as a photo of a meal plan, a grocery list or a list for the hardware store, and feeds them to whichever feature they belong to.                                                                                                                                       |
| **Notifications** | How anything reaches the household. Features and the agent decide what to say; this capability decides how it gets there.                                                                                                                                                                                              |

The agent is part of every feature, not a separate one (see [The "Blueberry Muffin" Approach](concepts-and-terminology.md#the-blueberry-muffin-approach-to-ai-integration)). For example:

- **Spends**: "These come in every month. I can cover them for you so you stop entering them."
- **Budgeting**: "You are always over on electricity and under on fuel. Move some from fuel to electricity."
- **Resources**: "The car hasn't been serviced in a year. Here's a proposal for a service."
- **Lists**: "You haven't made this meal in a while, and you have the ingredients from the last shop."

## Principles

- **One household.** Menlo is not multi-tenant. A rental unit is part of the household, not a household of its own.
- **Exact money.** Money never loses or invents a cent, never mixes currencies, and a recorded amount never changes after the fact. See the [Money API](../reference/money-api.md) for how.
- **Local AI preferred.** AI runs on the household's own server where it can. An external service is used only when local processing can't do the job.
- **Phone and computer.** Every feature works on both, and Menlo can be installed on a phone.
- **Real-time updates.** When one person changes something, everyone else sees it without refreshing.
- **Cost-conscious hosting.** Menlo runs cheaply on a home server (see [ADR-004](../decisions/adr-004-linux-deployment-host.md)).
- **Manual entry is acceptable.** Entering data by hand is fine for the MVP.
- **South African context.** Rands, and South African tax, payroll and banking terms.

Menlo assumes a steady internet connection. It doesn't work offline.

## What success looks like

- Both the COO and the CFO find Menlo worth using.
- The household plans and coordinates better.
- Money is less stressful because it is visible.
- Big decisions, like letting a rental unit, rest on accurate numbers.

## Finding the work

Each feature and capability is an **Area** on the [Menlo Roadmap](https://github.com/users/DigiBanks99/projects/2) project, beside **Platform** for work that is neither. Requirements still being shaped show in Backlog or Refining. See [Work Items](../reference/work-items.md) for how a Requirement goes from a stub to ready for an agent.
