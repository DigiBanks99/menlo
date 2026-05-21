import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  MnlButtonComponent,
  MnlButtonSize,
  MnlButtonType,
  MnlButtonVariant,
} from './button.component';

@Component({
  standalone: true,
  imports: [MnlButtonComponent],
  template: `
    <mnl-button
      [disabled]="disabled"
      [loading]="loading"
      [size]="size"
      [type]="type"
      [variant]="variant"
      (pressed)="handlePress($event)"
    >
      Save changes
    </mnl-button>
  `,
})
class TestHostComponent {
  variant: MnlButtonVariant = 'primary';
  size: MnlButtonSize = 'md';
  type: MnlButtonType = 'button';
  disabled = false;
  loading = false;
  readonly handlePress = vi.fn();
}

describe('MnlButtonComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it.each([['primary'], ['secondary'], ['ghost'], ['destructive']] satisfies [MnlButtonVariant][])(
    'renders the %s variant on a native button',
    (variant) => {
      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.componentInstance.variant = variant;
      fixture.detectChanges();

      const button = getButton(fixture);

      expect(button.tagName).toBe('BUTTON');
      expect(button.dataset.variant).toBe(variant);
      expect(button.textContent?.trim()).toBe('Save changes');
    },
  );

  it('applies the configured size and type attributes', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.size = 'lg';
    fixture.componentInstance.type = 'submit';
    fixture.detectChanges();

    const button = getButton(fixture);

    expect(button.dataset.size).toBe('lg');
    expect(button.getAttribute('type')).toBe('submit');
  });

  it('emits a pressed event when clicked while enabled', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();

    getButton(fixture).click();

    expect(fixture.componentInstance.handlePress).toHaveBeenCalledTimes(1);
  });

  it('suppresses clicks and marks aria-disabled when disabled', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const button = getButton(fixture);
    button.click();

    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-disabled')).toBe('true');
    expect(fixture.componentInstance.handlePress).not.toHaveBeenCalled();
  });

  it('prevents default and stops propagation when the protected click handler is reached while disabled', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlButtonComponent;
    const event = createMouseEvent();

    component['handleClick'](event as MouseEvent);

    expect(event.preventDefault).toHaveBeenCalledTimes(1);
    expect(event.stopImmediatePropagation).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.handlePress).not.toHaveBeenCalled();
  });

  it('shows a spinner, marks aria-busy, and suppresses clicks when loading', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.loading = true;
    fixture.detectChanges();

    const button = getButton(fixture);
    button.click();

    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-busy')).toBe('true');
    expect(fixture.nativeElement.querySelector('[data-testid="mnl-button-spinner"]')).toBeTruthy();
    expect(fixture.componentInstance.handlePress).not.toHaveBeenCalled();
  });

  it('prevents default and stops propagation when loading routes through the click handler', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.loading = true;
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlButtonComponent;
    const event = createMouseEvent();

    component['handleClick'](event as MouseEvent);

    expect(event.preventDefault).toHaveBeenCalledTimes(1);
    expect(event.stopImmediatePropagation).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.handlePress).not.toHaveBeenCalled();
  });
});

function getButton(fixture: { nativeElement: HTMLElement }): HTMLButtonElement {
  return fixture.nativeElement.querySelector('button') as HTMLButtonElement;
}

function createMouseEvent(): Pick<MouseEvent, 'preventDefault' | 'stopImmediatePropagation'> {
  return {
    preventDefault: vi.fn(),
    stopImmediatePropagation: vi.fn(),
  };
}
