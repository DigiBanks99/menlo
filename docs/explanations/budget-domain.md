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

## Rental units

From [Spec: Rental units and the rental view](https://github.com/DigiBanks99/menlo/issues/742):

- A **rental unit** (`Menlo.Lib.Budget.Entities.RentalUnit`) is a part of the property that the household lets to a tenant. It has an ID and a name. It belongs to the household, not to a budget, so it carries across years. It is audited, and not soft-deleted.
- Its **name** is a label of 1 to 100 characters, with leading and trailing spaces dropped. Renaming it is an edit, not a dated change, and never changes its identity. Names don't have to be unique.

### Rental unit endpoints

Like the budget endpoints, every signed-in member may use them, they answer 404 while `Features:Budget` is off, and errors are problem details with a `code`.

| Endpoint                        | Answers                                                                                              |
| ------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `POST /api/rental-units`        | Body `{ "name": "Garden flat" }`. 201 with `{ id, name }` and a `Location` of `/api/rental-units/{id}`; 400 `RentalUnit.InvalidName` for a blank name or one over 100 characters |
| `GET /api/rental-units`         | 200 with every rental unit, `[{ id, name }]`, by name                                                |
| `GET /api/rental-units/{id}`    | 200 with `{ id, name }`, or 404                                                                       |
| `PATCH /api/rental-units/{id}`  | Body `{ "name": "Cottage" }`. 200 with `{ id, name }`; a name left out keeps the one it has; 400 `RentalUnit.InvalidName`; 404 for an unknown ID |

`RentalUnitHandlers` in `src/api/Menlo.Api/Budget` does the work behind them.

## Not built yet

- Categories, lines, rental shares, utility recovery, the rental view and the budget page. `Features:Budget` shows or hides the Budgets navigation item, and its address, `/budgets`, shows the "not found" page until the new budget page is added. CD's default for the flag is `false`.
