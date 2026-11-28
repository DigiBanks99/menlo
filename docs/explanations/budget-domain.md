# Budget Domain

The budget is being rebuilt. The first model, with a budget's draft, active and closed states, budget items with planned, realized and spent amounts, and payer and attribution splits, was removed by [Spec: Remove the shipped budget](https://github.com/DigiBanks99/menlo/issues/670), together with its endpoints, its pages and its tables. Its data was test data and was dropped.

What the new budget does, and why, is in [Requirement: Budgeting](https://github.com/DigiBanks99/menlo/issues/551); its terms are in [`GLOSSARY.md`](../../GLOSSARY.md). Each Spec of that Requirement writes back here what it built.

Until then:

- There is no budget code in `src/lib/Menlo.Lib` or `src/lib/Menlo.Application`, no budget endpoint, and no budget table.
- `Features:Budget` shows or hides the Budgets navigation item. Its address, `/budgets`, shows the "not found" page until the new budget page is added. CD's default for the flag is `false`.
