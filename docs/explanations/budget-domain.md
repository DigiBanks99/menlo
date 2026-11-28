# Budget Domain

The budget is being rebuilt. The first model, with a budget's draft, active and closed states, budget items with planned, realized and spent amounts, and payer and attribution splits, was removed by [Spec: Remove the shipped budget](https://github.com/DigiBanks99/menlo/issues/670), together with its endpoints, its pages and its tables. Its data was test data and was dropped.

What the new budget does, and why, is in [Requirement: Budgeting](https://github.com/DigiBanks99/menlo/issues/551); its terms are in [`GLOSSARY.md`](../../GLOSSARY.md). Each Spec of that Requirement writes back here what it built.

## A budget for a year

From [Spec: A budget year and its categories](https://github.com/DigiBanks99/menlo/issues/671):

- A **budget** (`Menlo.Lib.Budget.Entities.Budget`) covers one calendar year, from 1 to 9999. It has an ID and its year, and no status: it is planned from the moment it's made. It is audited, and not soft-deleted.
- There is **one budget per year**. The handler refuses a second one, and the unique index on `budget.budgets.year` refuses one that slips past the check, for example when two members create the same year at once.

### Endpoints

Every signed-in member may use them. While `Features:Budget` is off they answer 404. Errors are problem details with a `code`.

| Endpoint                  | Answers                                                                                              |
| ------------------------- | ---------------------------------------------------------------------------------------------------- |
| `POST /api/budgets`       | Body `{ "year": 2027 }`. 201 with `{ id, year }` and a `Location` of `/api/budgets/{year}`; 409 `Budget.YearTaken` if the year has a budget; 400 `Budget.InvalidYear` for a year outside 1 to 9999 |
| `GET /api/budgets/{year}` | 200 with `{ id, year }`, or 404 if the year has no budget                                             |

`BudgetHandlers` in `src/api/Menlo.Api/Budget` does the work behind them, and the budget page calls the same handlers.

### The current month

`BudgetCalendar` in `Menlo.Lib.Budget` works out the **current month** by South African time (Africa/Johannesburg), from the app's `TimeProvider`, so a month freezes at local midnight on the 1st. It also says where a month of a year stands: **frozen** (before the current month), current, or ahead. Tests pin the clock with `TestWebApplicationFactory.Now`.

### The budget page

`/budgets` (in `src/api/Menlo.Api/Ui/Budgets`) shows one year, from `?year=`, or the current year when that is missing or not a budget year. Like the JSON endpoints, it answers with the "not found" page while `Features:Budget` is off, and every signed-in member may use it.

- **A year with no budget** offers to create it. The button posts the year to `POST /budgets`, which creates it with `BudgetHandlers` and goes to the year's page with a toast. If someone else made it first, it goes there all the same; a year that can't be a budget year gets an error toast.
- **The year picker** lists the years that have a budget, plus the year shown. It is hidden while no year has one.
- **A year with a budget** shows its frame, from the views prototype's variant D:
  - the **month strip**: the twelve months and the year, with frozen months shaded and the current month marked (`aria-current="date"`)
  - the **year grid**: a Category column, a column per month marked the same way, and the year. It is empty until categories are built.
  - the **side rail**: a column of icons on the grid's left edge for proposals, triggers, the set-up checklist, the year tools, "Who it is for", and how to read the grid. An icon opens its section in a panel over the grid. The open section is in the URL (`?panel=`), and htmx swaps only the frame. The sections are empty until their Specs fill them.

## Not built yet

- Categories and lines, and what the month strip, grid and side rail show of them. CD's default for `Features:Budget` is `false`.
