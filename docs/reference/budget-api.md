# Budget API Reference

The budget endpoints live under `/api/budgets`. The rules behind them are in [Budget Domain](../explanations/budget-domain.md).

## Common behaviour

- **Sign-in.** Every endpoint needs a signed-in user, and the API makes sure their record exists. The handlers resolve the user through `IUserContextProvider` and return `401` if that fails (see [Authentication API](authentication-api.md#resolving-the-current-user)).
- **Policies.** Reads need `CanViewBudget`; changes need `CanEditBudget`.
- **Shared.** There is one household, so every signed-in member sees and changes the same budgets.
- **Feature flags.** Endpoints return `404` when their flag is off: `Features:Budget` for budgets and categories, `Features:BudgetItems` for items and the summary.
- **Anti-forgery.** `POST`, `PUT` and `DELETE` need the `X-XSRF-TOKEN` header (see [Anti-forgery](authentication-api.md#anti-forgery)).
- **Errors.** Validation failures return `400` with a problem-details body.

## Budgets

| Endpoint                                 | Policy          | Does                                                                                                                                                    |
| ---------------------------------------- | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET /api/budgets`                       | `CanViewBudget` | Lists every budget: `id`, `year`, `status`, `categoryCount`, and the current month's `plannedExpenses` and `spentExpenses` (the totals need `Features:BudgetItems`). |
| `POST /api/budgets/{year}`               | `CanEditBudget` | Creates the budget for `year`, copying the previous year's categories. Returns `201`, or `200` with the existing budget. `400` unless `year` is this year or next. |
| `GET /api/budgets/{id}`                  | `CanViewBudget` | Gets a budget with its categories.                                                                                                                      |
| `POST /api/budgets/{id}/activate`        | `CanEditBudget` | Activates a Draft budget and closes last year's Active one. `400` if it isn't Draft.                                                                    |
| `GET /api/budgets/{budgetId}/summary`    | `CanViewBudget` | Totals by root category for income and expense. Optional `month` (1 to 12); without it, the whole year.                                                 |

The budget body has `id`, `year`, `status`, `categories` and `totalPlannedMonthlyAmount`. `totalPlannedMonthlyAmount` is the current month's planned income minus planned expenses. Each category's `plannedMonthlyAmount` is always zero, because categories hold no amounts.

## Categories

Under `/api/budgets/{budgetId}/categories`.

| Endpoint                           | Policy          | Does                                                                     |
| ---------------------------------- | --------------- | ------------------------------------------------------------------------ |
| `POST /`                           | `CanEditBudget` | Adds a category, at the root or under `parentId`.                         |
| `GET /`                            | `CanViewBudget` | Lists the category tree. `includeDeleted` (default `false`) adds deleted ones. |
| `GET /{categoryId}`                | `CanViewBudget` | Gets one category.                                                       |
| `PUT /{categoryId}`                | `CanEditBudget` | Updates name, flow, attribution, description, contributor and payer.     |
| `PUT /{categoryId}/reparent`       | `CanEditBudget` | Moves the category under another root, or to the root.                   |
| `DELETE /{categoryId}`             | `CanEditBudget` | Soft-deletes the category and its children.                              |
| `PUT /{categoryId}/restore`        | `CanEditBudget` | Restores the category and its children.                                  |

| Error                                      | Status |
| ------------------------------------------ | ------ |
| `DuplicateCategoryNameError`               | `409`  |
| `CategoryNotFoundError`                    | `404`  |
| `CategoryDepthError`, `DeletedParentError` | `400`  |
| `InvalidBudgetDataError`                   | `400`  |

## Items

Under `/api/budgets/{budgetId}/categories/{categoryId}/items`.

| Endpoint                   | Policy          | Does                                                                                          |
| -------------------------- | --------------- | --------------------------------------------------------------------------------------------- |
| `POST /`                   | `CanEditBudget` | Adds the item for one month.                                                                  |
| `POST /bulk`               | `CanEditBudget` | Adds an item to every month that doesn't have one.                                            |
| `POST /fill-forward`       | `CanEditBudget` | Sets the amount and splits from a start month to December, creating or updating items.        |
| `GET /`                    | `CanViewBudget` | Lists the category's items. Optional `month`.                                                 |
| `PUT /{itemId}`            | `CanEditBudget` | Updates any of the planned, realized and spent amounts and the splits.                        |
| `PUT /{itemId}/realize`    | `CanEditBudget` | Records the realized amount.                                                                  |
| `PUT /{itemId}/spent`      | `CanEditBudget` | Records the spent amount.                                                                     |
| `DELETE /{itemId}`         | `CanEditBudget` | Soft-deletes the item. It can't be restored.                                                  |

To create an item, send `month`, `budgetFlow` (`Income` or `Expense`), `plannedAmount`, `plannedCurrency`, a `payerSplit` of `{ userId, percent }` and an `attributionSplit` of `{ attribution, percent }`. `adjustmentRuleId` is optional.

| Error                                                                                     | Status |
| ----------------------------------------------------------------------------------------- | ------ |
| `DuplicateBudgetItemError`                                                                | `409`  |
| `CategoryNotFoundError`, `BudgetItemNotFoundError`                                        | `404`  |
| `NonLeafCategoryError`, `InvalidBudgetFlowError`, `InvalidMonthError`                     | `400`  |
| `InvalidPayerSplitError`, `InvalidAttributionSplitError`, an invalid flow or amount       | `400`  |

## Domain errors

`BudgetError` (`Menlo.Lib.Budget.Errors`) derives from `Error`. Codes have the form `Budget.<Name>`, for example `Budget.DuplicateCategoryName`.
