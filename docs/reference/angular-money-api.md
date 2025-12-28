# Angular Money - API Reference

## Overview

This document provides the complete API reference for working with Money values in Angular. The Money types are distributed across two libraries to maintain separation of concerns.

## Libraries

| Library       | Purpose           | Contains                                 |
| ------------- | ----------------- | ---------------------------------------- |
| `shared-util` | Core domain types | Money interface, MoneyUtils, type guards |
| `menlo-lib`   | UI components     | MoneyPipe                                |

## Money Interface

### Definition

```typescript
interface Money {
  readonly amount: number;
  readonly currency: string;
}
```

### Properties

#### `amount`

- **Type**: `number`
- **Description**: The numeric amount of money
- **Constraints**: Must be a valid JavaScript number
- **Notes**: JavaScript uses IEEE 754 floating-point. Never perform arithmetic operations in the frontend.

#### `currency`

- **Type**: `string`
- **Description**: The ISO 4217 currency code (e.g., 'ZAR', 'USD')
- **Constraints**: Must be a non-empty string
- **Format**: Typically 3 uppercase letters

### JSON Representation

```json
{
  "amount": 1234.56,
  "currency": "ZAR"
}
```

## MoneyUtils Class

Utility class providing helper methods for working with Money values.

### Static Methods

#### `create(amount: number, currency: string): Money`

Creates a Money instance from primitive values.

**Parameters:**

- `amount` - The numeric amount
- `currency` - The currency code

**Returns:** A Money object

**Example:**

```typescript
const budget = MoneyUtils.create(2000, 'ZAR');
// { amount: 2000, currency: 'ZAR' }
```

---

#### `zero(currency: string): Money`

Creates a Money instance with zero amount.

**Parameters:**

- `currency` - The currency code

**Returns:** A Money object with amount 0

**Example:**

```typescript
const zero = MoneyUtils.zero('ZAR');
// { amount: 0, currency: 'ZAR' }
```

---

#### `format(money: Money, locale?: string): string`

Formats a Money value for display using `Intl.NumberFormat`.

**Parameters:**

- `money` - The Money value to format
- `locale` - Optional locale string (defaults to 'en-ZA')

**Returns:** Formatted string with currency symbol

**Example:**

```typescript
const amount: Money = { amount: 1234.56, currency: 'ZAR' };

MoneyUtils.format(amount);              // "R 1,234.56"
MoneyUtils.format(amount, 'en-US');     // "ZAR 1,234.56"
```

**Notes:**

- Uses `Intl.NumberFormat` with `style: 'currency'`
- Always formats with 2 decimal places
- Browser-dependent formatting

---

#### `equals(a: Money, b: Money): boolean`

Compares two Money values for equality.

**Parameters:**

- `a` - First Money value
- `b` - Second Money value

**Returns:** `true` if both amount and currency match

**Example:**

```typescript
const a: Money = { amount: 100, currency: 'ZAR' };
const b: Money = { amount: 100, currency: 'ZAR' };
const c: Money = { amount: 100, currency: 'USD' };

MoneyUtils.equals(a, b);  // true
MoneyUtils.equals(a, c);  // false
```

---

#### `isZero(money: Money): boolean`

Checks if a Money value is zero.

**Parameters:**

- `money` - The Money value to check

**Returns:** `true` if amount is 0

**Example:**

```typescript
const zero = MoneyUtils.zero('ZAR');
const nonZero = MoneyUtils.create(100, 'ZAR');

MoneyUtils.isZero(zero);     // true
MoneyUtils.isZero(nonZero);  // false
```

---

#### `isPositive(money: Money): boolean`

Checks if a Money value is positive.

**Parameters:**

- `money` - The Money value to check

**Returns:** `true` if amount is greater than 0

**Example:**

```typescript
const positive = MoneyUtils.create(100, 'ZAR');
const zero = MoneyUtils.zero('ZAR');
const negative = MoneyUtils.create(-100, 'ZAR');

MoneyUtils.isPositive(positive);  // true
MoneyUtils.isPositive(zero);      // false
MoneyUtils.isPositive(negative);  // false
```

---

#### `isNegative(money: Money): boolean`

Checks if a Money value is negative.

**Parameters:**

- `money` - The Money value to check

**Returns:** `true` if amount is less than 0

**Example:**

```typescript
const negative = MoneyUtils.create(-100, 'ZAR');
const zero = MoneyUtils.zero('ZAR');
const positive = MoneyUtils.create(100, 'ZAR');

MoneyUtils.isNegative(negative);  // true
MoneyUtils.isNegative(zero);      // false
MoneyUtils.isNegative(positive);  // false
```

---

#### `sameCurrency(a: Money, b: Money): boolean`

Checks if two Money values have the same currency.

**Parameters:**

- `a` - First Money value
- `b` - Second Money value

**Returns:** `true` if currencies match

**Example:**

```typescript
const zar1 = MoneyUtils.create(100, 'ZAR');
const zar2 = MoneyUtils.create(200, 'ZAR');
const usd = MoneyUtils.create(100, 'USD');

MoneyUtils.sameCurrency(zar1, zar2);  // true
MoneyUtils.sameCurrency(zar1, usd);   // false
```

## Type Guards

### `isMoney(value: unknown): value is Money`

Type guard to check if a value is a valid Money object.

**Parameters:**

- `value` - The value to check

**Returns:** Type predicate - `true` if value matches Money interface

**Validation:**

- Checks if value is an object
- Verifies `amount` property exists and is a number
- Verifies `currency` property exists and is a non-empty string

**Example:**

```typescript
function processAmount(value: unknown) {
  if (isMoney(value)) {
    // TypeScript now knows value is Money
    console.log(value.amount);    // ✅ Type-safe
    console.log(value.currency);  // ✅ Type-safe
  }
}

isMoney({ amount: 100, currency: 'ZAR' });  // true
isMoney({ amount: '100', currency: 'ZAR' }); // false - amount is string
isMoney({ amount: 100 });                    // false - missing currency
isMoney(null);                               // false
```

---

### `isMoneyOrNull(value: unknown): value is Money | null`

Type guard to check if a value is Money or null.

**Parameters:**

- `value` - The value to check

**Returns:** Type predicate - `true` if value is Money or null

**Example:**

```typescript
function processOptional(value: unknown) {
  if (isMoneyOrNull(value)) {
    // TypeScript knows value is Money | null
    if (value === null) {
      console.log('No amount');
    } else {
      console.log(value.amount);  // ✅ Type-safe
    }
  }
}

isMoneyOrNull(null);                          // true
isMoneyOrNull({ amount: 100, currency: 'ZAR' }); // true
isMoneyOrNull(undefined);                     // false
```

## MoneyPipe

Angular pipe for formatting Money values in templates.

### Usage

```typescript
import { MoneyPipe } from 'menlo-lib';

@Component({
  imports: [MoneyPipe],
  // ...
})
```

### Transform Method

```typescript
transform(value: Money | null | undefined, locale?: string): string
```

**Parameters:**

- `value` - The Money value to format (or null/undefined)
- `locale` - Optional locale string for formatting

**Returns:** Formatted string, or empty string if value is null/undefined

**Template Syntax:**

```html
<!-- Default locale (en-ZA) -->
{{ amount | money }}

<!-- Custom locale -->
{{ amount | money:'en-US' }}

<!-- Handles null/undefined -->
{{ optionalAmount | money }}
```

**Example:**

```typescript
@Component({
  template: `
    <p>Budget: {{ budget() | money }}</p>
    <p>USD: {{ usd() | money:'en-US' }}</p>
  `
})
export class Example {
  budget = signal<Money>({ amount: 1234.56, currency: 'ZAR' });
  usd = signal<Money>({ amount: 100, currency: 'USD' });
}
```

## Type Definitions

### Import Paths

```typescript
// Core types and utilities
import {
  Money,
  MoneyUtils,
  isMoney,
  isMoneyOrNull
} from 'shared-util';

// UI components
import { MoneyPipe } from 'menlo-lib';
```

### TypeScript Configuration

Ensure your `tsconfig.json` includes the path mappings:

```json
{
  "compilerOptions": {
    "paths": {
      "shared-util": ["./dist/shared-util"],
      "menlo-lib": ["./dist/menlo-lib"]
    }
  }
}
```

## Usage Patterns

### With Angular Signals

```typescript
import { signal, computed } from '@angular/core';
import { Money, MoneyUtils } from 'shared-util';

const budget = signal<Money>(MoneyUtils.create(2000, 'ZAR'));
const spent = signal<Money>(MoneyUtils.create(1500, 'ZAR'));

// For display only - don't calculate in frontend
const display = computed(() => ({
  budget: MoneyUtils.format(budget()),
  spent: MoneyUtils.format(spent())
}));
```

### With RxJS

```typescript
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Money, MoneyUtils } from 'shared-util';

function formatMoney(source: Observable<Money>): Observable<string> {
  return source.pipe(
    map(money => MoneyUtils.format(money))
  );
}
```

### In Form Models

```typescript
interface BudgetForm {
  category: string;
  planned: Money | null;
}

const form = signal<BudgetForm>({
  category: 'Groceries',
  planned: MoneyUtils.create(2000, 'ZAR')
});
```

## Important Constraints

### ⚠️ No Arithmetic Operations

**NEVER perform arithmetic operations on Money in the frontend:**

```typescript
// ❌ WRONG - Never do this
const total = MoneyUtils.create(
  a.amount + b.amount,
  'ZAR'
);

// ✅ CORRECT - Let server calculate
interface Summary {
  total: Money;  // Calculated by backend
}
```

### Reasons

1. **Precision**: JavaScript floating-point arithmetic is imprecise
2. **Currency Safety**: Backend enforces currency validation
3. **Business Logic**: Complex calculations belong server-side
4. **Auditability**: Financial operations must be logged

## Related Documentation

- [Angular Money How-To Guide](../guides/angular-money-howto.md)
- [Money Domain Specifications](../requirements/money-domain/specifications.md)
- [Backend Money API Reference](./money-api.md)
