# Menlo

Menlo is a household management app. This glossary holds the language of its domain, starting with budgeting and spend.

## Budgeting and spend

**Budget**:
A household's committed statement, for one year, of what it expects to earn and spend.
_Avoid_: Budget file, spreadsheet

**Budget line**:
One thing the household earns or spends on, such as water or a salary. It has a planned amount for each month of the budget's year.
_Avoid_: Budget item (for the line as a whole), row

**Payer**:
The household member who pays for a budget line, or one of several who share it by percentage. Who pays changes by dated change.
_Avoid_: Owner, responsible person

**Personal view**:
One household member's cash flow: their income, their pre-tax deductions, the share of budget lines they pay, and their own net.
_Avoid_: My sheet, user budget

**Household view**:
The sum of every personal view.
_Avoid_: Main sheet, consolidated view

**Pre-tax deduction**:
An amount taken from a household member's gross salary before it is paid out, such as a pension contribution. It sits between gross and net pay, not among expenses.

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

**Baseline**:
A budget line's regular monthly amount, before any temporary variation.
_Avoid_: Default amount

**Dated change**:
A committed change to a budget line that applies from a given month onwards and never to earlier months.
_Avoid_: Edit, fill forward, cascade

**Yearly change**:
A dated change that recurs in the same month every year, such as an annual rates increase. Copying a budget plans it again in the new year.

**Year copy**:
Creating next year's budget from this year's: the baseline carries every dated change applied, while actuals and overrides are left behind.
_Avoid_: Clone, rollover

**Seasonal variation**:
A relative change to a budget line in the same months every year, expressed as a percentage or amount on top of the baseline.

**Conditional change**:
A relative change to one or more budget lines that applies from a trigger's expected date onwards.

**Trigger**:
A household event with an expected date that may move, such as a tenant moving in. Moving the date moves every conditional change tied to it.

**Override**:
An amount typed over a budget line's calculated planned amount for one month. It is rare and always visible.

**Line type**:
How a budget line's actuals are judged against its plan: fixed, variable, provision or event.

**Fixed line**:
A budget line whose actual should match its planned amount every month, such as a levy.

**Variable line**:
A budget line whose planned amount is a monthly allowance that actuals are expected to swing around, such as fuel. It is judged on its trend. Under-spend does not carry into the next month.
_Avoid_: Envelope

**Provision**:
A budget line with an annual amount but unknown timing, judged on its year-to-date actuals.
_Avoid_: Sinking fund, buffer

**Event line**:
A budget line with planned amounts only in known months, such as a yearly service.
_Avoid_: One-off adjustment

**Commit**:
The owner's decision to make something part of the budget. The plan only changes by commit, never as a side effect of actuals.
_Avoid_: Accept, approve, realize

**Proposal**:
A named, uncommitted set of changes to the budget (event lines, dated changes or conditional changes) that can be kept, switched on to see its effect, and later committed or dropped.
_Avoid_: Scenario, draft, plan

**Planned amount**:
What the budget expects for a budget line in a given month. It is frozen once that month is in the past.
_Avoid_: Budgeted amount, estimate

**Frozen month**:
A month before the current month. Its planned amounts can no longer change.
_Avoid_: Closed month, locked month

**Actual**:
The sum of spends recorded against a budget line, or a category, in a given month. Spends can be recorded for any month, including frozen ones.
_Avoid_: Spent amount, realized amount

**Spend**:
One recorded movement of money (date, amount, and the budget line or category it belongs to), entered by hand or imported from a bank statement. One spend can be split across several lines.
_Avoid_: Transaction (for the domain concept), expense entry

**Variance**:
The difference between the actual and the planned amount for a budget line in a month.
_Avoid_: Over-budget amount, delta
