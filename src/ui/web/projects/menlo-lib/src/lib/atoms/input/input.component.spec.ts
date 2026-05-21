import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlInputComponent, MnlInputType, MnlInputValue } from './input.component';

@Component({
  standalone: true,
  imports: [MnlInputComponent],
  template: `
    <mnl-input
      [disabled]="disabled"
      [error]="error"
      [placeholder]="placeholder"
      [type]="type"
      (valueChange)="handleValueChange($event)"
    >
      <span mnlInputLeadingIcon>R</span>
      <button mnlInputTrailingIcon type="button">Clear</button>
    </mnl-input>
  `,
})
class StandaloneInputHostComponent {
  disabled = false;
  error: boolean | string | null = null;
  placeholder = 'Household name';
  type: MnlInputType = 'text';
  readonly handleValueChange = vi.fn();
}

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, MnlInputComponent],
  template: `
    <mnl-input
      [error]="error"
      [placeholder]="placeholder"
      [type]="type"
      [formControl]="control"
      (valueChange)="handleValueChange($event)"
    ></mnl-input>
  `,
})
class ReactiveInputHostComponent {
  control = new FormControl<MnlInputValue>('Starting value');
  error: boolean | string | null = null;
  placeholder = 'Search budgets';
  type: MnlInputType = 'text';
  readonly handleValueChange = vi.fn();
}

describe('MnlInputComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StandaloneInputHostComponent, ReactiveInputHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it.each([['text'], ['email'], ['password'], ['search'], ['number']] satisfies [MnlInputType][])(
    'renders the %s input type on the native control',
    (type) => {
      const fixture = TestBed.createComponent(StandaloneInputHostComponent);
      fixture.componentInstance.type = type;
      fixture.detectChanges();

      const input = getInput(fixture);

      expect(input.getAttribute('type')).toBe(type);
      expect(input.getAttribute('placeholder')).toBe('Household name');
    },
  );

  it('renders values written through the ControlValueAccessor contract', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.componentInstance.type = 'number';
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlInputComponent;
    component.writeValue(1200);
    fixture.detectChanges();

    expect(getInput(fixture).value).toBe('1200');

    component.writeValue(null);
    fixture.detectChanges();

    expect(getInput(fixture).value).toBe('');
  });

  it('renders projected leading and trailing slot content', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.detectChanges();

    expect(getField(fixture).textContent).toContain('R');
    expect(fixture.nativeElement.querySelector('[mnlInputTrailingIcon]')?.textContent).toContain(
      'Clear',
    );
  });

  it('keeps text inputs empty for null writes and allows blur before forms registration', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlInputComponent;
    component.writeValue(null);
    component['handleBlur']();
    fixture.detectChanges();

    expect(getInput(fixture).value).toBe('');
  });

  it('emits string value changes while enabled', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.detectChanges();

    const input = getInput(fixture);
    input.value = 'Reconciled budget';
    input.dispatchEvent(new Event('input'));

    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith('Reconciled budget');
  });

  it('emits numeric values for number inputs and right-aligns the control', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.componentInstance.type = 'number';
    fixture.detectChanges();

    const input = getInput(fixture);
    input.value = '2450';
    input.dispatchEvent(new Event('input'));

    expect(input.className).toContain('text-right');
    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith(2450);
  });

  it('normalizes numeric string writes and empty or invalid numeric values to null', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.componentInstance.type = 'number';
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlInputComponent;
    component.writeValue('3200');
    fixture.detectChanges();
    expect(getInput(fixture).value).toBe('3200');

    component.writeValue('not-a-number');
    fixture.detectChanges();
    expect(getInput(fixture).value).toBe('');

    const input = getInput(fixture);
    input.value = '';
    input.dispatchEvent(new Event('input'));

    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith(null);
  });

  it('suppresses input handling and marks disabled state when disabled', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const input = getInput(fixture);
    input.value = 'Ignored';
    input.dispatchEvent(new Event('input'));

    expect(input.disabled).toBe(true);
    expect(getField(fixture).getAttribute('aria-disabled')).toBe('true');
    expect(fixture.componentInstance.handleValueChange).not.toHaveBeenCalled();
  });

  it('guards the input handler when disabled state reaches the component method', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlInputComponent;
    component['handleInput']({ target: { value: 'Blocked', valueAsNumber: Number.NaN } } as Event);

    expect(fixture.componentInstance.handleValueChange).not.toHaveBeenCalled();
  });

  it('applies error styling and aria-invalid when the error input is set', () => {
    const fixture = TestBed.createComponent(StandaloneInputHostComponent);
    fixture.componentInstance.error = 'Required';
    fixture.detectChanges();

    const field = getField(fixture);

    expect(field.className).toContain('ring-mnl-red');
    expect(getInput(fixture).getAttribute('aria-invalid')).toBe('true');
  });

  it('integrates with FormControl value updates and touched state via ControlValueAccessor', () => {
    const fixture = TestBed.createComponent(ReactiveInputHostComponent);
    fixture.componentInstance.control.setValue('Budget dashboard');
    fixture.detectChanges();

    const input = getInput(fixture);
    expect(input.value).toBe('Budget dashboard');

    input.value = 'Updated dashboard';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));

    expect(fixture.componentInstance.control.value).toBe('Updated dashboard');
    expect(fixture.componentInstance.control.touched).toBe(true);
    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith('Updated dashboard');
  });

  it('propagates disabled state from FormControl through setDisabledState', () => {
    const fixture = TestBed.createComponent(ReactiveInputHostComponent);
    fixture.componentInstance.control.disable();
    fixture.detectChanges();

    expect(getInput(fixture).disabled).toBe(true);
  });
});

function getField(fixture: { nativeElement: HTMLElement }): HTMLLabelElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-input-field"]') as HTMLLabelElement;
}

function getInput(fixture: { nativeElement: HTMLElement }): HTMLInputElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-input"]') as HTMLInputElement;
}
