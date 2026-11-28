# Budget Domain

The budget is being rebuilt. The first model, with a budget's draft, active and closed states, budget items with planned, realized and spent amounts, and payer and attribution splits, was removed by [Spec: Remove the shipped budget](https://github.com/DigiBanks99/menlo/issues/670), together with its endpoints, its pages and its tables. Its data was test data and was dropped.

What the new budget does, and why, is in [Requirement: Budgeting](https://github.com/DigiBanks99/menlo/issues/551); its terms are in [`GLOSSARY.md`](../../GLOSSARY.md). Each Spec of that Requirement writes back here what it built.

## A budget for a year

From [Spec: A budget year and its categories](https://github.com/DigiBanks99/menlo/issues/671):

- A **budget** (`Menlo.Lib.Budget.Entities.Budget`) covers one calendar year, from 1 to 9999. It has an ID and its year, and no status: it is planned from the moment it's made. It is audited, and not soft-deleted.
- There is **one budget per year**. The handler refuses a second one, and the unique index on `budget.budgets.year` refuses one that slips past the check, for example when two members create the same year at once.
- A budget has **categories** (`Menlo.Lib.Budget.Entities.Category`), added through `Budget.AddCategory`. A category has an ID, a name and an optional parent. They nest **at most two deep**: a top-level category may have subcategories, and a subcategory can't have its own. A name is trimmed, from 1 to 100 characters, and unique among its siblings, ignoring case, so "Insurance" may sit under both Home and Holiday. Categories are audited, not soft-deleted, and stored in `budget.categories`; a budget always loads with them.

### Endpoints

Every signed-in member may use them. While `Features:Budget` is off they answer 404. Errors are problem details with a `code`.

| Endpoint                  | Answers                                                                                              |
| ------------------------- | ---------------------------------------------------------------------------------------------------- |
| `POST /api/budgets`       | Body `{ "year": 2027 }`. 201 with `{ id, year, categories }` and a `Location` of `/api/budgets/{year}`; 409 `Budget.YearTaken` if the year has a budget; 400 `Budget.InvalidYear` for a year outside 1 to 9999 |
| `GET /api/budgets/{year}` | 200 with `{ id, year, categories }`, or 404 if the year has no budget                                 |
| `POST /api/budgets/{year}/categories` | Body `{ "name": "Electricity", "parentId": "<top-level category id>" }`, `parentId` optional. 201 with `{ id, name, parentId, children }` and a `Location` of `/api/budgets/{year}/categories/{id}`; 400 `Budget.InvalidCategoryName`; 404 `Budget.NotFound` if the year has no budget; 409 `Budget.CategoryNameTaken` if a sibling has the name; 422 `Budget.CategoryTooDeep` for a third level, or `Budget.CategoryNotFound` for a parent the budget doesn't have |

A budget's `categories` are its top-level categories, by name, each with its subcategories, by name, in `children`.

`BudgetHandlers` in `src/api/Menlo.Api/Budget` does the work behind them, so the budget page can call the same handlers.

## Not built yet

- Renaming, moving and removing categories, lines and the budget page. `Features:Budget` shows or hides the Budgets navigation item, and its address, `/budgets`, shows the "not found" page until the new budget page is added. CD's default for the flag is `false`.
