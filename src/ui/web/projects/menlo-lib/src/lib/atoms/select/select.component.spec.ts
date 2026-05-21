import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlSelectComponent, MnlSelectOption } from './select.component';

const options: readonly MnlSelectOption[] = [
  { value: 'income', label: 'Income' },
  { value: 'expense', label: 'Expense' },
  { value: 'savings', label: 'Savings', disabled: true },
];

@Component({
  standalone: true,
  imports: [MnlSelectComponent],
  template: `
    <mnl-select
      [disabled]="disabled"
      [error]="error"
      [options]="options"
      [placeholder]="placeholder"
      (valueChange)="handleValueChange($event)"
    >
      <span mnlSelectLeadingIcon>R</span>
    </mnl-select>
  `,
})
class StandaloneSelectHostComponent {
  disabled = false;
  error: boolean | string | null = null;
  options = options;
  placeholder = 'Choose a flow';
  readonly handleValueChange = vi.fn();
}

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, MnlSelectComponent],
  template: `
    <mnl-select
      [error]="error"
      [options]="options"
      [placeholder]="placeholder"
      [formControl]="control"
      (valueChange)="handleValueChange($event)"
    ></mnl-select>
  `,
})
class ReactiveSelectHostComponent {
  control = new FormControl<string | null>('income');
  error: boolean | string | null = null;
  options = options;
  placeholder = 'Choose a flow';
  readonly handleValueChange = vi.fn();
}

@Component({
  standalone: true,
  imports: [MnlSelectComponent],
  template: `
    <mnl-select placeholder="Projected options">
      <option value="expense">Expense</option>
      <option value="income">Income</option>
    </mnl-select>
  `,
})
class ProjectedOptionsHostComponent {}

describe('MnlSelectComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        MnlSelectComponent,
        StandaloneSelectHostComponent,
        ReactiveSelectHostComponent,
        ProjectedOptionsHostComponent,
      ],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('skips DOM syncing before the internal select element exists', () => {
    const fixture = TestBed.createComponent(MnlSelectComponent);
    Object.assign(fixture.componentInstance, { selectElement: () => undefined });

    expect(() => fixture.componentInstance.ngAfterViewChecked()).not.toThrow();
  });

  it('renders a styled native select with placeholder and configured options', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.detectChanges();

    const select = getSelect(fixture);
    const renderedOptions = select.querySelectorAll('option');

    expect(select.tagName).toBe('SELECT');
    expect(select.className).toContain('form-select');
    expect(renderedOptions).toHaveLength(4);
    expect(renderedOptions[0]?.textContent?.trim()).toBe('Choose a flow');
    expect((renderedOptions[3] as HTMLOptionElement).disabled).toBe(true);
  });

  it('renders projected option content when options are supplied via content projection', () => {
    const fixture = TestBed.createComponent(ProjectedOptionsHostComponent);
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlSelectComponent;
    component.writeValue('expense');
    fixture.detectChanges();

    const select = getSelect(fixture);
    const renderedOptions = select.querySelectorAll('option');

    expect(renderedOptions).toHaveLength(3);
    expect(renderedOptions[1]?.textContent?.trim()).toBe('Expense');
    expect(select.value).toBe('expense');
  });

  it('renders values written through the ControlValueAccessor contract', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlSelectComponent;
    component.writeValue('expense');
    fixture.detectChanges();

    expect(getSelect(fixture).value).toBe('expense');

    component.writeValue(null);
    fixture.detectChanges();

    expect(getSelect(fixture).value).toBe('');
  });

  it('renders projected leading slot content', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.detectChanges();

    expect(getField(fixture).textContent).toContain('R');
  });

  it('keeps placeholder selections empty and allows blur before forms registration', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlSelectComponent;
    component.writeValue('');
    component['handleBlur']();
    fixture.detectChanges();

    expect(getSelect(fixture).value).toBe('');
  });

  it('emits value changes when a selection is made', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.detectChanges();

    const select = getSelect(fixture);
    select.value = 'expense';
    select.dispatchEvent(new Event('change'));

    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith('expense');
  });

  it('emits null when the placeholder option is selected', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.detectChanges();

    const select = getSelect(fixture);
    select.value = '';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith(null);
    expect(select.value).toBe('');
  });

  it('suppresses selection changes and marks disabled state when disabled', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const select = getSelect(fixture);
    select.value = 'expense';
    select.dispatchEvent(new Event('change'));

    expect(select.disabled).toBe(true);
    expect(getField(fixture).getAttribute('aria-disabled')).toBe('true');
    expect(fixture.componentInstance.handleValueChange).not.toHaveBeenCalled();
  });

  it('guards the change handler when disabled state reaches the component method', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlSelectComponent;
    component['handleChange']({ target: { value: 'expense' } } as Event);

    expect(fixture.componentInstance.handleValueChange).not.toHaveBeenCalled();
  });

  it('applies error styling and aria-invalid when the error input is set', () => {
    const fixture = TestBed.createComponent(StandaloneSelectHostComponent);
    fixture.componentInstance.error = 'Required';
    fixture.detectChanges();

    expect(getField(fixture).className).toContain('ring-mnl-red');
    expect(getSelect(fixture).getAttribute('aria-invalid')).toBe('true');
  });

  it('integrates with FormControl value updates and touched state via ControlValueAccessor', () => {
    const fixture = TestBed.createComponent(ReactiveSelectHostComponent);
    fixture.componentInstance.control.setValue('expense');
    fixture.detectChanges();

    const select = getSelect(fixture);
    expect(select.value).toBe('expense');

    select.value = 'income';
    select.dispatchEvent(new Event('change'));
    select.dispatchEvent(new Event('blur'));

    expect(fixture.componentInstance.control.value).toBe('income');
    expect(fixture.componentInstance.control.touched).toBe(true);
    expect(fixture.componentInstance.handleValueChange).toHaveBeenCalledWith('income');
  });

  it('propagates disabled state from FormControl through setDisabledState', () => {
    const fixture = TestBed.createComponent(ReactiveSelectHostComponent);
    fixture.componentInstance.control.disable();
    fixture.detectChanges();

    expect(getSelect(fixture).disabled).toBe(true);
  });
});

function getField(fixture: { nativeElement: HTMLElement }): HTMLLabelElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-select-field"]',
  ) as HTMLLabelElement;
}

function getSelect(fixture: { nativeElement: HTMLElement }): HTMLSelectElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-select"]') as HTMLSelectElement;
}
