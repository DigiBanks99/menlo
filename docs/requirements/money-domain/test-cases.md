# Money Domain Object - Test Cases

## 1. Unit Tests

### 1.1. Creation & Equality
- [ ] **TC-MON-001**: Create `Money` with valid amount and currency.
- [ ] **TC-MON-002**: Verify two `Money` objects with same amount and currency are equal.
- [ ] **TC-MON-003**: Verify two `Money` objects with different amount are not equal.
- [ ] **TC-MON-004**: Verify two `Money` objects with different currency are not equal.
- [ ] **TC-MON-005**: Verify `Money` cannot be created with null currency (if applicable).

### 1.2. Arithmetic Operations
- [ ] **TC-MON-006**: **Add**: `10 ZAR + 20 ZAR` returns Success with `30 ZAR`.
- [ ] **TC-MON-007**: **Add**: `10 ZAR + 20 USD` returns Failure with `CurrencyMismatchError`.
- [ ] **TC-MON-008**: **Subtract**: `30 ZAR - 10 ZAR` returns Success with `20 ZAR`.
- [ ] **TC-MON-009**: **Subtract**: `30 ZAR - 10 USD` returns Failure with `CurrencyMismatchError`.
- [ ] **TC-MON-010**: **Multiply**: `10 ZAR * 2 = 20 ZAR`.
- [ ] **TC-MON-011**: **Multiply**: `10 ZAR * 2.5 = 25 ZAR`.
- [ ] **TC-MON-012**: **Divide**: `10 ZAR / 2` returns Success with `5 ZAR`.
- [ ] **TC-MON-013**: **Divide**: `10 ZAR / 3` returns Success and handles rounding correctly (e.g., 3.33).
- [ ] **TC-MON-013b**: **Divide**: `10 ZAR / 0` returns Failure with `DivisionByZeroError`.

### 1.3. Allocation
- [ ] **TC-MON-014**: **Allocate Evenly**: `1.00 ZAR` split into 3 parts -> `[0.34, 0.33, 0.33]`. Sum must be `1.00`.
- [ ] **TC-MON-015**: **Allocate Evenly**: `0.05 ZAR` split into 2 parts -> `[0.03, 0.02]`.
- [ ] **TC-MON-016**: **Allocate Ratios**: `100 ZAR` split by ratio `[1, 3]` -> `[25, 75]`.
- [ ] **TC-MON-017**: **Allocate Ratios**: `0.04 ZAR` split by ratio `[30, 70]` -> `[0.01, 0.03]`.

### 1.4. Comparison
- [ ] **TC-MON-018**: `10 ZAR > 5 ZAR` is true.
- [ ] **TC-MON-019**: `10 ZAR < 5 ZAR` is false.
- [ ] **TC-MON-020**: `10 ZAR >= 10 ZAR` is true.
- [ ] **TC-MON-021**: Comparison with different currencies returns Failure (if using explicit method) or throws (if using operators). Prefer explicit `Compare` method returning `Result`.

## 2. Integration Tests

### 2.1. Persistence (EF Core)
- [ ] **TC-MON-INT-001**: Save entity with `Money` property to database.
- [ ] **TC-MON-INT-002**: Retrieve entity and verify `Money` property values (Amount, Currency).
- [ ] **TC-MON-INT-003**: Query entities based on `Money` amount (e.g., `Where(x => x.Price.Amount > 100)`).

### 2.2. Serialization
- [ ] **TC-MON-INT-004**: Serialize `Money` to JSON.
- [ ] **TC-MON-INT-005**: Deserialize JSON to `Money`.
