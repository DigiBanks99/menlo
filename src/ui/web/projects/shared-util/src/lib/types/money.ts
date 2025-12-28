/**
 * Represents a monetary value with currency.
 * This interface matches the JSON serialization from the backend Money value object.
 *
 * @remarks
 * The backend uses decimal precision. JavaScript uses IEEE 754 floating-point.
 * Always perform calculations server-side for financial operations.
 * This interface is for display and transport only.
 */
export interface Money {
  readonly amount: number;
  readonly currency: string;
}

/**
 * Type guard to check if an object is a valid Money instance.
 *
 * @param value - The value to check
 * @returns True if the value matches the Money interface
 */
export function isMoney(value: unknown): value is Money {
  return (
    typeof value === 'object' &&
    value !== null &&
    'amount' in value &&
    'currency' in value &&
    typeof (value as Money).amount === 'number' &&
    typeof (value as Money).currency === 'string' &&
    (value as Money).currency.length > 0
  );
}

/**
 * Type guard to check if a value might be Money (nullable).
 *
 * @param value - The value to check
 * @returns True if the value is null or matches the Money interface
 */
export function isMoneyOrNull(value: unknown): value is Money | null {
  return value === null || isMoney(value);
}

/**
 * Utilities for working with Money values in the UI.
 *
 * @remarks
 * These utilities are for display purposes only.
 * Never perform financial calculations in the frontend.
 * All arithmetic must be done server-side.
 */
export class MoneyUtils {
  /**
   * Creates a Money instance from primitive values.
   *
   * @param amount - The numeric amount
   * @param currency - The currency code (e.g., 'ZAR')
   * @returns A Money object
   */
  static create(amount: number, currency: string): Money {
    return { amount, currency };
  }

  /**
   * Creates a zero Money value for a given currency.
   *
   * @param currency - The currency code
   * @returns A Money object with amount 0
   */
  static zero(currency: string): Money {
    return { amount: 0, currency };
  }

  /**
   * Formats Money for display using Intl.NumberFormat.
   *
   * @param money - The Money value to format
   * @param locale - Optional locale (defaults to 'en-ZA' for South Africa)
   * @returns Formatted string (e.g., 'R 100.00')
   */
  static format(money: Money, locale: string = 'en-ZA'): string {
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: money.currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(money.amount);
  }

  /**
   * Compares two Money values for equality.
   *
   * @param a - First Money value
   * @param b - Second Money value
   * @returns True if amounts and currencies match
   */
  static equals(a: Money, b: Money): boolean {
    return a.amount === b.amount && a.currency === b.currency;
  }

  /**
   * Checks if a Money value is zero.
   *
   * @param money - The Money value to check
   * @returns True if amount is 0
   */
  static isZero(money: Money): boolean {
    return money.amount === 0;
  }

  /**
   * Checks if a Money value is positive.
   *
   * @param money - The Money value to check
   * @returns True if amount is greater than 0
   */
  static isPositive(money: Money): boolean {
    return money.amount > 0;
  }

  /**
   * Checks if a Money value is negative.
   *
   * @param money - The Money value to check
   * @returns True if amount is less than 0
   */
  static isNegative(money: Money): boolean {
    return money.amount < 0;
  }

  /**
   * Checks if two Money values have the same currency.
   *
   * @param a - First Money value
   * @param b - Second Money value
   * @returns True if currencies match
   */
  static sameCurrency(a: Money, b: Money): boolean {
    return a.currency === b.currency;
  }
}
