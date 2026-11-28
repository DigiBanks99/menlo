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

`BudgetHandlers` in `src/api/Menlo.Api/Budget` does the work behind them, so the budget page can call the same handlers.

## Not built yet

- Categories, lines and the budget page. `Features:Budget` shows or hides the Budgets navigation item, and its address, `/budgets`, shows the "not found" page until the new budget page is added. CD's default for the flag is `false`.
