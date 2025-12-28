import { TestBed } from '@angular/core/testing';
import { Money } from 'shared-util';
import { beforeEach, describe, expect, it } from 'vitest';
import { MoneyPipe } from './money.pipe';

describe('MoneyPipe', () => {
  let pipe: MoneyPipe;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [MoneyPipe],
    });
    pipe = TestBed.inject(MoneyPipe);
  });

  it('should create an instance', () => {
    expect(pipe).toBeTruthy();
  });

  it('should format Money value', () => {
    const money: Money = { amount: 100, currency: 'ZAR' };
    const result = pipe.transform(money);

    expect(result).toContain('100');
  });

  it('should return empty string for null', () => {
    const result = pipe.transform(null);

    expect(result).toBe('');
  });

  it('should return empty string for undefined', () => {
    const result = pipe.transform(undefined);

    expect(result).toBe('');
  });

  it('should format with custom locale', () => {
    const money: Money = { amount: 1234.56, currency: 'USD' };
    const result = pipe.transform(money, 'en-US');

    expect(result).toContain('1,234.56');
  });
});
