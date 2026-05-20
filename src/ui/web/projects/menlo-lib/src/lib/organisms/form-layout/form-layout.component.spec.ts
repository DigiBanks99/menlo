import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlFormLayoutComponent } from './form-layout.component';

@Component({
  standalone: true,
  imports: [MnlFormLayoutComponent],
  template: `
    <mnl-form-layout>
      <div class="space-y-2" data-testid="host-title" mnlFormTitle>
        <h2 class="m-0 text-2xl font-semibold">Budget details</h2>
        <p class="m-0 text-sm text-mnl-subtext">Create a monthly household line item.</p>
      </div>

      <section class="space-y-4" data-testid="primary-section">
        <label class="block">
          <span class="block text-sm font-medium">Title</span>
          <input data-testid="title-input" type="text" />
        </label>
      </section>

      <section class="grid gap-4 md:grid-cols-2" data-testid="secondary-section">
        <label class="block">
          <span class="block text-sm font-medium">Planned amount</span>
          <input type="text" />
        </label>

        <label class="block">
          <span class="block text-sm font-medium">Category</span>
          <select>
            <option>Groceries</option>
          </select>
        </label>
      </section>

      <div data-testid="host-actions" mnlFormActions>
        <button data-testid="clear-button" type="button">Clear</button>
        <button data-testid="save-button" type="submit">Save item</button>
      </div>
    </mnl-form-layout>
  `,
})
class FormLayoutHostComponent {}

describe('MnlFormLayoutComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormLayoutHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('projects the title, sections, and actions into the correct slots', () => {
    const fixture = TestBed.createComponent(FormLayoutHostComponent);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const title = host.querySelector('[data-testid="mnl-form-layout-title"]');
    const sections = host.querySelector('[data-testid="mnl-form-layout-sections"]');
    const actions = host.querySelector('[data-testid="mnl-form-layout-actions"]');

    expect(title?.textContent).toContain('Budget details');
    expect(sections?.querySelector('[data-testid="primary-section"]')).toBeTruthy();
    expect(sections?.querySelector('[data-testid="secondary-section"]')).toBeTruthy();
    expect(actions?.querySelector('[data-testid="clear-button"]')?.textContent?.trim()).toBe(
      'Clear',
    );
    expect(actions?.querySelector('[data-testid="save-button"]')?.textContent?.trim()).toBe(
      'Save item',
    );
  });

  it('renders a sticky action bar with visual separation from the form body', () => {
    const fixture = TestBed.createComponent(FormLayoutHostComponent);
    fixture.detectChanges();

    const actions = fixture.nativeElement.querySelector(
      '[data-testid="mnl-form-layout-actions"]',
    ) as HTMLElement;

    expect(actions.className).toContain('sticky');
    expect(actions.className).toContain('bottom-0');
    expect(actions.className).toContain('border-t');
    expect(actions.className).toContain('bg-mnl-surface/95');
  });
});
