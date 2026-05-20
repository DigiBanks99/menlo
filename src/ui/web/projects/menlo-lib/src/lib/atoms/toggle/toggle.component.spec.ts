import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlToggleComponent } from './toggle.component';

@Component({
  standalone: true,
  imports: [MnlToggleComponent],
  template: `
    <mnl-toggle
      [checked]="checked"
      [disabled]="disabled"
      (checkedChange)="handleCheckedChange($event)"
    >
      Push notifications
    </mnl-toggle>
  `,
})
class StandaloneToggleHostComponent {
  checked = false;
  disabled = false;
  readonly handleCheckedChange = vi.fn();
}

@Component({
  standalone: true,
  imports: [MnlToggleComponent],
  template: ` <mnl-toggle [label]="label"></mnl-toggle> `,
})
class LabelToggleHostComponent {
  label = 'Budget reminders';
}

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, MnlToggleComponent],
  template: `
    <mnl-toggle [formControl]="control" (checkedChange)="handleCheckedChange($event)">
      Household alerts
    </mnl-toggle>
  `,
})
class ReactiveToggleHostComponent {
  control = new FormControl<boolean>(true, { nonNullable: true });
  readonly handleCheckedChange = vi.fn();
}

describe('MnlToggleComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LabelToggleHostComponent,
        ReactiveToggleHostComponent,
        StandaloneToggleHostComponent,
      ],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders as a switch button with aria-checked and projected label content', () => {
    const fixture = TestBed.createComponent(StandaloneToggleHostComponent);
    fixture.detectChanges();

    const toggle = getToggle(fixture);

    expect(toggle.tagName).toBe('BUTTON');
    expect(toggle.getAttribute('role')).toBe('switch');
    expect(toggle.getAttribute('aria-checked')).toBe('false');
    expect(toggle.textContent).toContain('Push notifications');
  });

  it('renders the optional label input text when configured', () => {
    const fixture = TestBed.createComponent(LabelToggleHostComponent);
    fixture.detectChanges();

    expect(getToggle(fixture).textContent).toContain('Budget reminders');
  });

  it('toggles state and emits changes when clicked while enabled', () => {
    const fixture = TestBed.createComponent(StandaloneToggleHostComponent);
    fixture.detectChanges();

    const toggle = getToggle(fixture);
    toggle.click();
    fixture.detectChanges();

    expect(toggle.getAttribute('aria-checked')).toBe('true');
    expect(toggle.dataset.state).toBe('on');
    expect(fixture.componentInstance.handleCheckedChange).toHaveBeenCalledWith(true);
  });

  it.each(['Enter', ' '] satisfies readonly string[])(
    'toggles with the %s keyboard interaction',
    (key) => {
      const fixture = TestBed.createComponent(StandaloneToggleHostComponent);
      fixture.detectChanges();

      const toggle = getToggle(fixture);
      toggle.dispatchEvent(new KeyboardEvent('keydown', { key }));
      fixture.detectChanges();

      expect(toggle.getAttribute('aria-checked')).toBe('true');
      expect(fixture.componentInstance.handleCheckedChange).toHaveBeenCalledWith(true);
    },
  );

  it('suppresses activation and marks aria-disabled when disabled', () => {
    const fixture = TestBed.createComponent(StandaloneToggleHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const toggle = getToggle(fixture);
    toggle.click();

    expect(toggle.disabled).toBe(true);
    expect(toggle.getAttribute('aria-disabled')).toBe('true');
    expect(toggle.getAttribute('aria-checked')).toBe('false');
    expect(fixture.componentInstance.handleCheckedChange).not.toHaveBeenCalled();
  });

  it('integrates with FormControl updates and touched state through ControlValueAccessor', () => {
    const fixture = TestBed.createComponent(ReactiveToggleHostComponent);
    fixture.detectChanges();

    const toggle = getToggle(fixture);
    expect(toggle.getAttribute('aria-checked')).toBe('true');

    toggle.click();
    toggle.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.componentInstance.control.value).toBe(false);
    expect(fixture.componentInstance.control.touched).toBe(true);
    expect(fixture.componentInstance.handleCheckedChange).toHaveBeenCalledWith(false);
  });

  it('propagates disabled state from FormControl through setDisabledState', () => {
    const fixture = TestBed.createComponent(ReactiveToggleHostComponent);
    fixture.componentInstance.control.disable();
    fixture.detectChanges();

    expect(getToggle(fixture).disabled).toBe(true);
  });
});

function getToggle(fixture: { nativeElement: HTMLElement }): HTMLButtonElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-toggle"]') as HTMLButtonElement;
}
