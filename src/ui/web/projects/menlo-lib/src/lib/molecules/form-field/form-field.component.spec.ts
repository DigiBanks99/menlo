import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlInputComponent } from '../../atoms/input';
import { MnlFormFieldComponent } from './form-field.component';

@Component({
  standalone: true,
  imports: [MnlFormFieldComponent, MnlInputComponent],
  template: `
    <mnl-form-field
      [error]="error"
      [hint]="hint"
      [inputId]="inputId"
      [label]="label"
      [required]="required"
    >
      <mnl-input [id]="inputId" placeholder="Budget name"></mnl-input>
    </mnl-form-field>
  `,
})
class FormFieldHostComponent {
  error: string | null = null;
  hint = 'Used on the monthly budget dashboard.';
  inputId = 'budget-name';
  label = 'Budget name';
  required = false;
}

describe('MnlFormFieldComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormFieldHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders the label and links it to the projected control id', () => {
    const fixture = TestBed.createComponent(FormFieldHostComponent);
    fixture.detectChanges();

    const label = getLabel(fixture);

    expect(label.textContent?.trim()).toBe('Budget name');
    expect(label.getAttribute('for')).toBe('budget-name');
    expect(getInput(fixture).id).toBe('budget-name');
  });

  it('shows the required marker when required is true', () => {
    const fixture = TestBed.createComponent(FormFieldHostComponent);
    fixture.componentInstance.required = true;
    fixture.detectChanges();

    expect(getLabel(fixture).textContent).toContain('*');
  });

  it('shows the hint text when provided', () => {
    const fixture = TestBed.createComponent(FormFieldHostComponent);
    fixture.detectChanges();

    expect(getHint(fixture)?.textContent?.trim()).toBe('Used on the monthly budget dashboard.');
  });

  it('shows the error text only when the error input is non-null', () => {
    const emptyFixture = TestBed.createComponent(FormFieldHostComponent);
    emptyFixture.detectChanges();

    expect(getError(emptyFixture)).toBeNull();

    const erroredFixture = TestBed.createComponent(FormFieldHostComponent);
    erroredFixture.componentInstance.error = 'Budget name is required';
    erroredFixture.detectChanges();

    expect(getError(erroredFixture)?.textContent?.trim()).toBe('Budget name is required');
  });
});

function getError(fixture: { nativeElement: HTMLElement }): HTMLElement | null {
  return fixture.nativeElement.querySelector('[data-testid="mnl-form-field-error"]');
}

function getHint(fixture: { nativeElement: HTMLElement }): HTMLElement | null {
  return fixture.nativeElement.querySelector('[data-testid="mnl-form-field-hint"]');
}

function getInput(fixture: { nativeElement: HTMLElement }): HTMLInputElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-input"]') as HTMLInputElement;
}

function getLabel(fixture: { nativeElement: HTMLElement }): HTMLLabelElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-form-field-label"]',
  ) as HTMLLabelElement;
}
