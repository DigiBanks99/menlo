import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlAmountInputComponent, MnlAmountInputValue } from './amount-input.component';

@Component({
  standalone: true,
  imports: [MnlAmountInputComponent],
  template: `
    <mnl-amount-input
      [currency]="currency"
      [disabled]="disabled"
      [error]="error"
      [id]="inputId"
      [placeholder]="placeholder"
      (valueChange)="handleValueChange($event)"
    ></mnl-amount-input>
  `,
})
class StandaloneAmountInputHostComponent {
  currency = 'ZAR';
  disabled = false;
  error: boolean | string | null = null;
  inputId = 'planned-amount';
  placeholder = '0.00';
  readonly handleValueChange = vi.fn();
}

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, MnlAmountInputComponent],
  template: `
    <mnl-amount-input
      [currency]="currency"
      [formControl]="control"
      (valueChange)="handleValueChange($event)"
    ></mnl-amount-input>
  `,
})
class ReactiveAmountInputHostComponent {
  control = new FormControl<MnlAmountInputValue>(1234.5);
  currency = 'ZAR';
  readonly handleValueChange = vi.fn();
}

describe('MnlAmountInputComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReactiveAmountInputHostComponent, StandaloneAmountInputHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('shows the ZAR prefix by default', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.detectChanges();

    expect(getPrefix(fixture).textContent?.trim()).toBe('R');
  });

  it('shows the configured currency code when the currency is not ZAR', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.componentInstance.currency = 'USD';
    fixture.detectChanges();

    expect(getPrefix(fixture).textContent?.trim()).toBe('USD');
  });

  it('renders values written through the ControlValueAccessor contract as formatted amounts', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlAmountInputComponent;
    component.writeValue(2450);
    fixture.detectChanges();

    expect(getInput(fixture).value).toBe(formatAmount(2450));
  });

  it('keeps written values editable while focused and clears null writes', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.detectChanges();

    const component = getPrivateComponent(fixture);
    component.handleFocus();
    component.writeValue(2450);
    fixture.detectChanges();

    expect(getInput(fixture).value).toBe('2450');

    component.writeValue(null);
    fixture.detectChanges();

    expect(getInput(fixture).value).toBe('');
  });

  it('emits raw numeric values while formatting the display on blur', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.detectChanges();

    const input = getInput(fixture);
    input.dispatchEvent(new Event('focus'));
    input.value = '12345.67';
    input.dispatchEvent(new Event('input'));

    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith(12345.67);

    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(input.value).toBe(formatAmount(12345.67));
  });

  it('parses grouped values and emits null for empty input', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.detectChanges();

    const input = getInput(fixture);
    input.dispatchEvent(new Event('focus'));
    input.value = '1,234.50';
    input.dispatchEvent(new Event('input'));
    input.value = '';
    input.dispatchEvent(new Event('input'));

    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith(1234.5);
    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith(null);
  });

  it('suppresses input handling and marks disabled state when disabled', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const input = getInput(fixture);
    input.value = '9999';
    input.dispatchEvent(new Event('input'));

    expect(input.disabled).toBe(true);
    expect(getField(fixture).className).toContain('cursor-not-allowed');
    expect(fixture.componentInstance.handleValueChange).not.toHaveBeenCalled();
  });

  it('ignores focus requests while disabled', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const component = getPrivateComponent(fixture);
    component.handleFocus();

    expect(getInput(fixture).value).toBe('');
  });

  it('applies error styling and aria-invalid when the error input is set', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.componentInstance.error = 'Required';
    fixture.detectChanges();

    expect(getField(fixture).className).toContain('ring-mnl-red');
    expect(getInput(fixture).getAttribute('aria-invalid')).toBe('true');
  });

  it('integrates with FormControl updates and touched state via ControlValueAccessor', () => {
    const fixture = TestBed.createComponent(ReactiveAmountInputHostComponent);
    fixture.detectChanges();

    const input = getInput(fixture);
    expect(input.value).toBe(formatAmount(1234.5));

    input.dispatchEvent(new Event('focus'));
    input.value = '6789.01';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.componentInstance.control.value).toBe(6789.01);
    expect(fixture.componentInstance.control.touched).toBe(true);
    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith(6789.01);
  });

  it('propagates disabled state from FormControl through setDisabledState', () => {
    const fixture = TestBed.createComponent(ReactiveAmountInputHostComponent);
    fixture.componentInstance.control.disable();
    fixture.detectChanges();

    expect(getInput(fixture).disabled).toBe(true);
  });

  it('normalizes and parses invalid edge-case values to null', () => {
    const fixture = TestBed.createComponent(StandaloneAmountInputHostComponent);
    fixture.detectChanges();

    const component = getPrivateComponent(fixture);

    expect(component.normalizeValue('2450')).toBe(2450);
    expect(component.normalizeValue('invalid')).toBeNull();
    expect(component.parseValue('-')).toBeNull();
    expect(component.parseValue('12.3.4')).toBeNull();
    expect(component.formatAmount(null)).toBe('');
  });
});

function formatAmount(value: number): string {
  return new Intl.NumberFormat('en-ZA', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function getField(fixture: { nativeElement: HTMLElement }): HTMLLabelElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-amount-input-field"]',
  ) as HTMLLabelElement;
}

function getInput(fixture: { nativeElement: HTMLElement }): HTMLInputElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-amount-input"]',
  ) as HTMLInputElement;
}

function getPrefix(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-amount-input-prefix"]',
  ) as HTMLElement;
}

function getPrivateComponent(fixture: {
  debugElement: { children: Array<{ componentInstance: unknown }> };
}): PrivateAmountInputComponent {
  return fixture.debugElement.children[0].componentInstance as PrivateAmountInputComponent;
}

type PrivateAmountInputComponent = MnlAmountInputComponent & {
  formatAmount: (value: MnlAmountInputValue) => string;
  handleFocus: () => void;
  normalizeValue: (value: unknown) => MnlAmountInputValue;
  parseValue: (value: string) => MnlAmountInputValue;
};
