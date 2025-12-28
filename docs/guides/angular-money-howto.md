# Angular Money - How-To Guide

## Overview

This guide shows you how to work with Money values in the Angular frontend, matching the JSON serialization format from the backend Money value object.

## Quick Start

### Installation

The Money types are distributed across two libraries:

- `shared-util` - Core types, utilities, and type guards
- `menlo-lib` - UI components (MoneyPipe)

### Basic Usage

```typescript
import { Money, MoneyUtils } from 'shared-util';

// Create a Money value
const budget = MoneyUtils.create(2000, 'ZAR');

// Format for display
const formatted = MoneyUtils.format(budget); // "R 2,000.00"
```

## Common Tasks

### Creating Money Values

```typescript
import { MoneyUtils } from 'shared-util';

// Create from amount and currency
const amount = MoneyUtils.create(1500.50, 'ZAR');

// Create a zero value
const zero = MoneyUtils.zero('ZAR');
```

### Validating API Responses

Use type guards to validate Money objects from API responses:

```typescript
import { isMoney, isMoneyOrNull } from 'shared-util';

interface BudgetDto {
  planned: Money | null;
  spent: Money;
}

function validateBudget(dto: BudgetDto): void {
  if (dto.planned !== null && !isMoney(dto.planned)) {
    throw new Error('Invalid planned amount');
  }
  
  if (!isMoney(dto.spent)) {
    throw new Error('Invalid spent amount');
  }
}
```

### Displaying Money in Templates

Use the `MoneyPipe` for formatting in templates:

```typescript
import { Component, signal } from '@angular/core';
import { Money, MoneyUtils } from 'shared-util';
import { MoneyPipe } from 'menlo-lib';

@Component({
  selector: 'app-budget',
  standalone: true,
  imports: [MoneyPipe],
  template: `
    <div class="budget">
      <p>Planned: {{ planned() | money }}</p>
      <p>Spent: {{ spent() | money }}</p>
      
      <!-- Custom locale -->
      <p>USD: {{ usdAmount() | money:'en-US' }}</p>
      
      <!-- Handles null/undefined -->
      <p>Optional: {{ optional() | money }}</p>
    </div>
  `
})
export class BudgetComponent {
  planned = signal<Money>(MoneyUtils.create(2000, 'ZAR'));
  spent = signal<Money>(MoneyUtils.create(1500, 'ZAR'));
  usdAmount = signal<Money>(MoneyUtils.create(100, 'USD'));
  optional = signal<Money | null>(null);
}
```

### Comparing Money Values

```typescript
import { MoneyUtils, Money } from 'shared-util';

const budget: Money = { amount: 2000, currency: 'ZAR' };
const spent: Money = { amount: 1500, currency: 'ZAR' };

// Check equality
if (MoneyUtils.equals(budget, spent)) {
  console.log('Amounts match');
}

// Check if zero
if (MoneyUtils.isZero(spent)) {
  console.log('Nothing spent');
}

// Check sign
if (MoneyUtils.isPositive(budget)) {
  console.log('Budget is positive');
}

if (MoneyUtils.isNegative(spent)) {
  console.log('Overspent!');
}

// Check currency match
if (MoneyUtils.sameCurrency(budget, spent)) {
  console.log('Same currency');
}
```

### Service Integration

Create services that fetch and validate Money from APIs:

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Money, isMoney } from 'shared-util';

interface CategoryResponse {
  id: string;
  name: string;
  planned: Money | null;
  spent: Money;
}

@Injectable({ providedIn: 'root' })
export class BudgetService {
  private readonly http = inject(HttpClient);

  getCategories(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>('/api/budgets/categories').pipe(
      map(categories => categories.map(cat => this.validate(cat)))
    );
  }

  private validate(dto: CategoryResponse): CategoryResponse {
    if (dto.planned !== null && !isMoney(dto.planned)) {
      throw new Error(`Invalid planned amount for category ${dto.name}`);
    }
    if (!isMoney(dto.spent)) {
      throw new Error(`Invalid spent amount for category ${dto.name}`);
    }
    return dto;
  }
}
```

### Formatting with Different Locales

The `MoneyUtils.format()` method uses `Intl.NumberFormat` internally:

```typescript
import { MoneyUtils, Money } from 'shared-util';

const amount: Money = { amount: 1234.56, currency: 'ZAR' };

// South African locale (default)
MoneyUtils.format(amount); // "R 1,234.56"

// US locale
MoneyUtils.format(amount, 'en-US'); // "$1,234.56" (if currency is USD)

// British locale
MoneyUtils.format(amount, 'en-GB'); // "£1,234.56" (if currency is GBP)
```

### Working with Signals

Store Money values in Angular signals for reactive updates:

```typescript
import { Component, signal, computed } from '@angular/core';
import { Money, MoneyUtils } from 'shared-util';
import { MoneyPipe } from 'menlo-lib';

@Component({
  selector: 'app-budget-tracker',
  standalone: true,
  imports: [MoneyPipe],
  template: `
    <div>
      <p>Budget: {{ budget() | money }}</p>
      <p>Spent: {{ spent() | money }}</p>
      
      @if (isOverBudget()) {
        <div class="warning">Over budget!</div>
      }
    </div>
  `
})
export class BudgetTrackerComponent {
  budget = signal<Money>(MoneyUtils.create(2000, 'ZAR'));
  spent = signal<Money>(MoneyUtils.create(2100, 'ZAR'));
  
  // IMPORTANT: This computed should display server-calculated values
  // Never calculate remaining = budget - spent in frontend!
  isOverBudget = computed(() => {
    // This logic should come from the server
    return this.spent().amount > this.budget().amount;
  });
}
```

## Important Rules

### ⚠️ NEVER Perform Arithmetic in Frontend

**All financial calculations MUST be done server-side.**

❌ **WRONG - Never do this:**

```typescript
// BAD - Do not perform arithmetic
const remaining = MoneyUtils.create(
  budget.amount - spent.amount,  // NEVER DO THIS!
  'ZAR'
);

const total = MoneyUtils.create(
  amount1.amount + amount2.amount,  // WRONG!
  'ZAR'
);
```

✅ **CORRECT - Let the server calculate:**

```typescript
interface BudgetSummary {
  budget: Money;
  spent: Money;
  remaining: Money;  // Calculated by server
}

@Injectable({ providedIn: 'root' })
export class BudgetService {
  getSummary(): Observable<BudgetSummary> {
    // Server returns all calculations
    return this.http.get<BudgetSummary>('/api/budgets/summary');
  }
}
```

### Why No Frontend Arithmetic?

1. **Precision Loss**: JavaScript uses IEEE 754 floating-point, which causes rounding errors in financial calculations
2. **Currency Safety**: The backend enforces currency matching and prevents invalid operations
3. **Business Logic**: Complex algorithms (penny allocation, tax calculations) must be server-side
4. **Audit Trail**: All financial operations need server-side logging for compliance
5. **Consistency**: Single source of truth for all financial calculations

## Testing

### Unit Testing Utilities

```typescript
import { describe, expect, it } from 'vitest';
import { MoneyUtils, Money } from 'shared-util';

describe('Budget Calculation Display', () => {
  it('should format budget correctly', () => {
    const budget: Money = { amount: 2000, currency: 'ZAR' };
    const formatted = MoneyUtils.format(budget);
    
    expect(formatted).toContain('2');
    expect(formatted).toContain('000');
  });

  it('should detect zero amounts', () => {
    const zero = MoneyUtils.zero('ZAR');
    
    expect(MoneyUtils.isZero(zero)).toBe(true);
    expect(MoneyUtils.isPositive(zero)).toBe(false);
  });

  it('should detect negative amounts', () => {
    const negative = MoneyUtils.create(-100, 'ZAR');
    
    expect(MoneyUtils.isNegative(negative)).toBe(true);
  });
});
```

### Component Testing

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { MoneyPipe } from 'menlo-lib';
import { BudgetComponent } from './budget.component';

describe('BudgetComponent', () => {
  let component: BudgetComponent;
  let fixture: ComponentFixture<BudgetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BudgetComponent, MoneyPipe],
    }).compileComponents();

    fixture = TestBed.createComponent(BudgetComponent);
    component = fixture.componentInstance;
  });

  it('should display formatted money', () => {
    fixture.detectChanges();
    const element = fixture.nativeElement;
    
    expect(element.textContent).toContain('R');
  });
});
```

### Type Guard Testing

```typescript
import { describe, expect, it } from 'vitest';
import { isMoney, isMoneyOrNull } from 'shared-util';

describe('Money Type Guards', () => {
  it('should validate Money objects', () => {
    const valid = { amount: 100, currency: 'ZAR' };
    const invalid = { amount: '100', currency: 'ZAR' };
    
    expect(isMoney(valid)).toBe(true);
    expect(isMoney(invalid)).toBe(false);
  });

  it('should handle null values', () => {
    expect(isMoneyOrNull(null)).toBe(true);
    expect(isMoneyOrNull({ amount: 100, currency: 'ZAR' })).toBe(true);
    expect(isMoneyOrNull(undefined)).toBe(false);
  });
});
```

## Troubleshooting

### Type Errors with Money

If TypeScript shows errors like "Property 'amount' does not exist":

```typescript
// Make sure you're importing from the correct library
import { Money } from 'shared-util';  // ✅ Correct

// Not from a component or service
import { Money } from './money.service';  // ❌ Wrong
```

### MoneyPipe Not Found

If the pipe isn't recognized in templates:

```typescript
import { MoneyPipe } from 'menlo-lib';  // ✅ Correct import

@Component({
  imports: [MoneyPipe],  // ✅ Must be in imports array
  // ...
})
```

### Runtime Validation Errors

If API responses fail validation:

1. Check the API response format matches `{ amount: number, currency: string }`
2. Ensure the backend is serializing Money correctly
3. Verify the DTO interface matches the API contract

## Related Documentation

- [Angular Money API Reference](../reference/angular-money-api.md)
- [Money Domain Specifications](../requirements/money-domain/specifications.md)
- [Money Domain Implementation Plan](../requirements/money-domain/implementation.md)
- [Money Backend API Reference](../reference/money-api.md)
- [Money Backend How-To Guide](./money-howto.md)
