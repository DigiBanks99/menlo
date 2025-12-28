import { describe, expect, it } from 'vitest';
import { isMoney, isMoneyOrNull, Money, MoneyUtils } from './money';

describe('MoneyUtils', () => {
  describe('create', () => {
    it('should create Money with amount and currency', () => {
      const money = MoneyUtils.create(100.5, 'ZAR');

      expect(money.amount).toBe(100.5);
      expect(money.currency).toBe('ZAR');
    });
  });

  describe('zero', () => {
    it('should create Money with zero amount', () => {
      const money = MoneyUtils.zero('ZAR');

      expect(money.amount).toBe(0);
      expect(money.currency).toBe('ZAR');
    });
  });

  describe('format', () => {
    it('should format Money for South African locale', () => {
      const money: Money = { amount: 1234.56, currency: 'ZAR' };
      const formatted = MoneyUtils.format(money);

      // Format: R 1,234.56 or R1,234.56 depending on browser
      expect(formatted).toContain('1');
      expect(formatted).toContain('234');
      expect(formatted).toContain('56');
    });

    it('should format Money with custom locale', () => {
      const money: Money = { amount: 1234.56, currency: 'USD' };
      const formatted = MoneyUtils.format(money, 'en-US');

      expect(formatted).toContain('1,234.56');
    });
  });

  describe('equals', () => {
    it('should return true for equal Money values', () => {
      const a: Money = { amount: 100, currency: 'ZAR' };
      const b: Money = { amount: 100, currency: 'ZAR' };

      expect(MoneyUtils.equals(a, b)).toBe(true);
    });

    it('should return false for different amounts', () => {
      const a: Money = { amount: 100, currency: 'ZAR' };
      const b: Money = { amount: 200, currency: 'ZAR' };

      expect(MoneyUtils.equals(a, b)).toBe(false);
    });

    it('should return false for different currencies', () => {
      const a: Money = { amount: 100, currency: 'ZAR' };
      const b: Money = { amount: 100, currency: 'USD' };

      expect(MoneyUtils.equals(a, b)).toBe(false);
    });
  });

  describe('isZero', () => {
    it('should return true for zero amount', () => {
      const money = MoneyUtils.zero('ZAR');

      expect(MoneyUtils.isZero(money)).toBe(true);
    });

    it('should return false for non-zero amount', () => {
      const money = MoneyUtils.create(100, 'ZAR');

      expect(MoneyUtils.isZero(money)).toBe(false);
    });
  });

  describe('isPositive', () => {
    it('should return true for positive amount', () => {
      const money = MoneyUtils.create(100, 'ZAR');

      expect(MoneyUtils.isPositive(money)).toBe(true);
    });

    it('should return false for zero amount', () => {
      const money = MoneyUtils.zero('ZAR');

      expect(MoneyUtils.isPositive(money)).toBe(false);
    });

    it('should return false for negative amount', () => {
      const money = MoneyUtils.create(-100, 'ZAR');

      expect(MoneyUtils.isPositive(money)).toBe(false);
    });
  });

  describe('isNegative', () => {
    it('should return true for negative amount', () => {
      const money = MoneyUtils.create(-100, 'ZAR');

      expect(MoneyUtils.isNegative(money)).toBe(true);
    });

    it('should return false for positive amount', () => {
      const money = MoneyUtils.create(100, 'ZAR');

      expect(MoneyUtils.isNegative(money)).toBe(false);
    });
  });

  describe('sameCurrency', () => {
    it('should return true for same currency', () => {
      const a = MoneyUtils.create(100, 'ZAR');
      const b = MoneyUtils.create(200, 'ZAR');

      expect(MoneyUtils.sameCurrency(a, b)).toBe(true);
    });

    it('should return false for different currencies', () => {
      const a = MoneyUtils.create(100, 'ZAR');
      const b = MoneyUtils.create(100, 'USD');

      expect(MoneyUtils.sameCurrency(a, b)).toBe(false);
    });
  });
});

describe('Money Type Guards', () => {
  describe('isMoney', () => {
    it('should return true for valid Money object', () => {
      const money = { amount: 100, currency: 'ZAR' };

      expect(isMoney(money)).toBe(true);
    });

    it('should return false for null', () => {
      expect(isMoney(null)).toBe(false);
    });

    it('should return false for undefined', () => {
      expect(isMoney(undefined)).toBe(false);
    });

    it('should return false for object missing amount', () => {
      const invalid = { currency: 'ZAR' };

      expect(isMoney(invalid)).toBe(false);
    });

    it('should return false for object missing currency', () => {
      const invalid = { amount: 100 };

      expect(isMoney(invalid)).toBe(false);
    });

    it('should return false for empty currency', () => {
      const invalid = { amount: 100, currency: '' };

      expect(isMoney(invalid)).toBe(false);
    });

    it('should return false for wrong types', () => {
      const invalid = { amount: '100', currency: 'ZAR' };

      expect(isMoney(invalid)).toBe(false);
    });
  });

  describe('isMoneyOrNull', () => {
    it('should return true for valid Money object', () => {
      const money = { amount: 100, currency: 'ZAR' };

      expect(isMoneyOrNull(money)).toBe(true);
    });

    it('should return true for null', () => {
      expect(isMoneyOrNull(null)).toBe(true);
    });

    it('should return false for undefined', () => {
      expect(isMoneyOrNull(undefined)).toBe(false);
    });

    it('should return false for invalid object', () => {
      const invalid = { amount: 100 };

      expect(isMoneyOrNull(invalid)).toBe(false);
    });
  });
});
