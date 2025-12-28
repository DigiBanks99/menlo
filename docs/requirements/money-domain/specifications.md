# Money Domain Object - Specifications

## 1. Overview

The `Money` domain object is a fundamental building block for the Menlo Home Management application. It encapsulates the concept of a monetary value, ensuring precision, currency safety, and correct
arithmetic operations. It replaces the use of raw `decimal` or `double` types for financial calculations to prevent precision errors and "primitive obsession".

## 2. Business Requirements

| ID             | Requirement              | Description                                                                                                                                                                                     | Priority |
| :------------- | :----------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :------- |
| **BR-MON-001** | **Precision & Accuracy** | All financial calculations must maintain precision to at least 2 decimal places, with internal calculations potentially using higher precision to avoid rounding errors until the final result. | High     |
| **BR-MON-002** | **Currency Safety**      | Operations between different currencies must be explicitly prevented. The system primarily supports ZAR (South African Rand).                                                                   | High     |
| **BR-MON-003** | **Allocation**           | The system must support splitting amounts into parts (e.g., monthly budgets, shared costs) without losing or creating cents (Penny Allocation problem).                                         | High     |
| **BR-MON-004** | **Immutability**         | Monetary values must be immutable to ensure thread safety and predictability in domain logic.                                                                                                   | High     |
| **BR-MON-005** | **Error Handling**       | Domain operations must return Result failure states with specific Error implementations (e.g., currency mismatch -> CurrencyMismatchError).                                                     | High     |

## 3. Functional Requirements

### 3.1. Data Structure

- **FR-MON-001**: The `Money` object must be a **Value Object**.
- **FR-MON-002**: It must contain an `Amount` (decimal) and a `Currency` (string/code).
- **FR-MON-003**: Two `Money` objects are equal if their `Amount` and `Currency` are equal.

### 3.2. Operations

- **FR-MON-004**: **Addition**: `Add(Money other) -> Result<Money, Error>`. Returns Failure if currencies differ.
- **FR-MON-005**: **Subtraction**: `Subtract(Money other) -> Result<Money, Error>`. Returns Failure if currencies differ.
- **FR-MON-006**: **Multiplication**: `Multiply(decimal factor) -> Money`. (Always succeeds).
- **FR-MON-007**: **Division**: `Divide(decimal divisor) -> Result<Money, Error>`. Returns Failure if divisor is zero. Rounding strategy: MidpointRounding.ToEven.
- **FR-MON-008**: **Comparison**: Support `>`, `<`, `>=`, `<=`. Note: C# operators may throw if currencies differ; prefer explicit `CompareTo` or ensure currency match before comparing.

### 3.3. Advanced Operations

**Why Allocation?**
When splitting money (e.g., dividing a R100.00 bill among 3 people), simple division results in R33.3333...
R33.33 * 3 = R99.99. We lose 1 cent.
The Allocation pattern distributes the remainder to ensure the sum of parts equals the original total.
This is critical for:

- **Attribution**: Splitting utility bills (e.g., 30% rental / 70% personal).
- **Budgeting**: Dividing annual budgets into months.

- **FR-MON-009**: **Allocate**: `Allocate(int parts) -> IEnumerable<Money>`. Distributes remainder to first `n` parts.
- **FR-MON-010**: **Ratio Allocation**: `Allocate(params int[] ratios) -> IEnumerable<Money>`. Distributes based on weights.

### 3.4. Persistence & Serialization

- **FR-MON-011**: Must be mappable to EF Core (Owned Entity or Complex Type).
- **FR-MON-012**: Must be serializable to/from JSON.

## 4. Acceptance Criteria

- **AC-MON-001**: `Money(10, "ZAR").Add(Money(5, "ZAR"))` returns Success with `Money(15, "ZAR")`.
- **AC-MON-002**: `Money(10, "ZAR").Add(Money(5, "USD"))` returns Failure with `CurrencyMismatchError`.
- **AC-MON-003**: `Money(100, "ZAR").Allocate(3)` returns `[33.34, 33.33, 33.33]`. Sum is 100.00.
- **AC-MON-004**: JSON serialization results in `{"amount": 10.00, "currency": "ZAR"}`.
- **AC-MON-005**: EF Core can save and retrieve entities with `Money` properties.

## 5. Constraints

- **C-MON-001**: Primary currency is ZAR.
- **C-MON-002**: No external exchange rate service integration.
