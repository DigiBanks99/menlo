# Menlo

Menlo is a household management app. This glossary holds the language of its domain, starting with budgeting and payments.

## Product

**Feature**:
An area of Menlo with its own subject, such as the household's money or its time, built end to end. Features are the verticals.
_Avoid_: Pillar, module

**Capability**:
Something any feature can use, such as lists or notifications. Capabilities are the horizontals.
_Avoid_: Pillar, module, shared feature

**Household**:
The one group of people Menlo serves. Menlo holds a single household's data, so the household is implied: there is no household record, and nothing in the domain, database or API is scoped by one.
_Avoid_: Family account, tenant, workspace

**Household member**:
A person who has signed in to Menlo. Sign-in is limited by the Entra app registration, so Menlo keeps no member list of its own; on first sign-in it creates the member's `User` record. Every member sees the same household data.
_Avoid_: Account holder, customer

## Budgeting and payments

**Budget**:
A household's committed statement, for one calendar year, of what it expects to earn and spend.
_Avoid_: Budget file, spreadsheet

**Budget line**:
One thing the household earns or spends on, such as water or a salary. It has a planned amount for each month of the budget's year.
_Avoid_: Budget item (for the line as a whole), row

**Payer**:
The household member who pays for a budget line, or one of several who share it by percentage. The payer split shares the line's plan between personal views; the payments each member actually makes share its actual. Who pays changes by dated change.
_Avoid_: Owner, responsible person

**Personal view**:
One household member's cash flow: their payslip and other income, the share of budget lines they pay, and their own net. Its plan follows the payer split; its actuals follow the payments the member made or received.
_Avoid_: My sheet, user budget

**Household view**:
The sum of every personal view.
_Avoid_: Main sheet, consolidated view

**Payslip**:
A household member's planned pay, as budget lines that each have a payslip kind: earning, company contribution, deduction before tax, tax, or deduction after tax. Payslip lines are planned like any line, but payments don't land on them; their plan stands in as their actual.
_Avoid_: Salary sheet

**Deduction before tax**:
A payslip line taken from earnings before tax, such as a member's own pension contribution. It lowers taxable income.
_Avoid_: Pre-tax deduction

**Company contribution**:
A payslip line the employer pays on a member's behalf, such as an employer pension contribution. It is never paid to the member, but it counts towards taxable income.

**Taxable income**:
A payslip's earnings plus company contributions, minus deductions before tax. It is a plain sum to help plan the tax line, not a tax calculation.

**Net pay**:
A payslip's earnings minus its deductions and tax: what the employer pays into the bank. It is a budget line whose plan is worked out from the payslip, and the salary payment is recorded on it.
_Avoid_: Take-home pay, salary (for the amount paid in)

**Net**:
Income minus expenses for a view and period. It is positive for a surplus and negative for a deficit.
_Avoid_: Balance, leftover

**Category**:
What a budget line is for, such as home, school or transport. Categories nest at most two deep, and budget lines sit in the lowest level.
_Avoid_: Worksheet, section

**Beneficiary**:
Who a budget line is for: a household member, or the household as a whole. It is a tag, not a level of grouping.
_Avoid_: Attribution, per-person category

**Rental unit**:
A part of the property that the household lets to a tenant.
_Avoid_: Flat, cottage

**Rental share**:
The percentage of a shared expense line that belongs to a rental unit. It changes by dated change.
_Avoid_: Attribution, attribution split

**Utility recovery**:
An income line of a rental unit: the money the household gets back for utilities a tenant used, recorded net, as received.
_Avoid_: Service provider share, rebate

**Rental view**:
A rental unit's income minus its rental shares of expenses, which is its rental profit.

**Tax year**:
The year March to February that tax is assessed on. It spans two budgets. The rental view reports a rental unit's rental profit for a tax year, from actuals for months that have passed and the plan for months to come.
_Avoid_: Financial year, fiscal year

**Baseline**:
A budget line's regular monthly amount, before any temporary variation.
_Avoid_: Default amount

**Dated change**:
A committed change to a budget line's baseline that applies from a given month onwards and never to earlier months. It can only start in the current month or later.
_Avoid_: Edit, fill forward, cascade

**Yearly change**:
A dated change that recurs in the same month every year, such as an annual rates increase. It moves the baseline and stays, unlike a seasonal variation. Copying a budget plans it again in the new year, with last year's amount or percentage until it is changed.

**Year copy**:
Creating next year's budget from this year's: the baseline carries every dated change applied, while actuals and overrides are left behind. The copy is planned from the moment it is made. It asks whether each trigger is realized and whether to keep each event line.
_Avoid_: Clone, rollover

**Seasonal variation**:
A relative change to a budget line in the same months every year, expressed as a percentage or amount on top of the baseline. Outside those months the line returns to its baseline.

**Conditional change**:
A relative change to one or more budget lines that applies from a trigger's expected date onwards.

**Trigger**:
An expected household event with a date that may move, such as a tenant moving in. Moving the date moves every conditional change tied to it, and the date can only be set to the current month or later. A trigger is still expected until it is realized.

**Realized trigger**:
A trigger whose expected event has actually happened. At year copy, a realized trigger's conditional changes fold into the new baseline; a trigger not yet realized carries over, and a date already past moves to January.

**Override**:
An amount typed over a budget line's calculated planned amount for one month. It is rare and always visible. It stays in place when the plan beneath it changes, and can only be set on the current month or later.

**Line type**:
How a budget line's actuals are judged against its plan: fixed, variable, provision or event.

**Judgement**:
How a budget line, a category or the net is doing against its plan: on plan, better than plan, worse than plan, or well worse than plan. Only recorded months count, and the current month shows only what is already certain, such as an expense already over. A judgement flags; it does not suggest.
_Avoid_: Status, health, score

**Fixed line**:
A budget line whose actual should match its planned amount every month, such as a levy. It is judged on its variance to date this year.

**Variable line**:
A budget line whose planned amount is a monthly allowance that actuals are expected to swing around, such as fuel. It is judged on its trend: its last three completed months of the year against their allowances, so one heavy month alone does not count. Under-spend does not carry into the next month.
_Avoid_: Envelope

**Provision**:
A budget line with an annual amount but unknown timing, judged on its year-to-date actuals against the annual amount. Because the timing is unknown, using it early is not worse than plan; only going over the annual amount is.
_Avoid_: Sinking fund, buffer

**Event line**:
A budget line with planned amounts only in known months, such as a yearly service. It is judged on its variance to date this year, so an event paid a month late nets out.
_Avoid_: One-off adjustment

**Commit**:
The owner's decision to make something part of the budget. The plan only changes by commit, never as a side effect of actuals. Saving a direct edit to a budget line commits it; a proposal is committed as a whole. A commit applies from the current month onwards.
_Avoid_: Accept, approve, realize

**Proposal**:
A named, uncommitted set of changes to the budget (event lines, dated changes or conditional changes) that can be kept, switched on to see its effect, and later committed or dropped. Dropping a proposal deletes it. Two proposals that change the same thing can't be switched on together.
_Avoid_: Scenario, draft, plan

**Suggestion**:
A proposal the agent wrote, with the reason it was made: what it noticed and the figures behind it. It aims to keep every budget line on plan and every net at zero or above, as far as it can.
_Avoid_: Recommendation, reallocation, option set

**Follow**:
A suggested change that moves a budget line's plan to what is actually being paid. Any line can be followed.

**Cut**:
A suggested change that plans a budget line below what is being paid, which only works if the household spends less. Only a line marked as one that can be cut is ever cut.
_Avoid_: Lock, minimum, earmark

**Planned amount**:
What the budget expects for a budget line in a given month: its baseline plus any seasonal variation and conditional changes, or its override if it has one. It is frozen once that month is in the past.
_Avoid_: Budgeted amount, estimate

**Frozen month**:
A month before the current month. Its planned amounts can no longer change.
_Avoid_: Closed month, locked month

**Actual**:
The sum of splits recorded against a budget line, or a category, in a given month. Expense and income payments raise it; refunds lower it. Payments can be recorded for any month, including frozen ones. A line with nothing recorded in a month takes its planned amount as its actual, unless an unsorted split in its category covers it.
_Avoid_: Spent amount, realized amount

**Payment**:
One recorded movement of money: a kind, a date, an amount, the one household member who made or received it, and an optional note, entered by hand or imported from a bank statement. Its amount is divided into one or more splits. Who a payment is for comes from the budget lines it is split onto.
_Avoid_: Spend, transaction (for the domain concept), expense entry

**Expense payment**:
A payment of money out, split onto expense lines.

**Income payment**:
A payment of money in, such as a salary or a utility recovery, split onto income lines.

**Refund**:
A payment of money back for an expense, split onto the expense lines it reverses. It lowers their actuals in the month it is recorded for.
_Avoid_: Negative payment, reversal

**Split**:
A part of a payment: an amount on one budget line, or on a lowest-level category when it is not yet sorted. A payment's splits add up to its amount, share its kind, and may sit in different categories.
_Avoid_: Allocation, line part

**Unsorted split**:
A split on a category instead of a budget line. It counts towards the category's actual but not towards any line's, and it may stay unsorted.
_Avoid_: Uncategorised payment, pending split

**Nothing this month**:
An R0 payment on one budget line's month, recording that nothing was paid or received, so its actual is R0 and its plan doesn't stand in. It is the only payment that may be R0.
_Avoid_: Skip, zero payment

**Variance**:
The difference between the actual and the planned amount for a budget line in a month.
_Avoid_: Over-budget amount, delta
