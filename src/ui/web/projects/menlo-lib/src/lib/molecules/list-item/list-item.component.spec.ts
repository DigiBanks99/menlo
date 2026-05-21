import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlListItemButtonType, MnlListItemComponent } from './list-item.component';

@Component({
  standalone: true,
  imports: [MnlListItemComponent],
  template: `
    <mnl-list-item
      [href]="href"
      [interactive]="interactive"
      [rel]="rel"
      [selected]="selected"
      [target]="target"
      [type]="type"
      (pressed)="handlePressed($event)"
    >
      <span mnlListItemLeading>AI</span>
      <div>
        <p class="title">Groceries</p>
        <p class="subtitle">Weekly household essentials</p>
      </div>
      <span mnlListItemTrailing>R 1 240</span>
    </mnl-list-item>
  `,
})
class ListItemHostComponent {
  href: string | null = null;
  interactive = false;
  rel: string | null = null;
  selected = false;
  target: string | null = null;
  type: MnlListItemButtonType = 'button';
  readonly handlePressed = vi.fn();
}

describe('MnlListItemComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListItemHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders the leading, body, and trailing slots', () => {
    const fixture = TestBed.createComponent(ListItemHostComponent);
    fixture.detectChanges();

    expect(getLeading(fixture).textContent?.trim()).toBe('AI');
    expect(getBody(fixture).textContent).toContain('Groceries');
    expect(getBody(fixture).textContent).toContain('Weekly household essentials');
    expect(getTrailing(fixture).textContent?.trim()).toBe('R 1 240');
  });

  it('renders as a non-interactive div by default', () => {
    const fixture = TestBed.createComponent(ListItemHostComponent);
    fixture.detectChanges();

    const item = getItem(fixture);
    expect(item.tagName).toBe('DIV');
    expect(item.dataset.interactive).toBe('false');
  });

  it('renders as an accessible button with hover styling when interactive', () => {
    const fixture = TestBed.createComponent(ListItemHostComponent);
    fixture.componentInstance.interactive = true;
    fixture.componentInstance.selected = true;
    fixture.detectChanges();

    const item = getItem(fixture);

    expect(item.tagName).toBe('BUTTON');
    expect(item.getAttribute('aria-pressed')).toBe('true');
    expect(item.className).toContain('hover:bg-mnl-surface-alt/80');
    expect(item.className).toContain('cursor-pointer');
  });

  it('emits the pressed event and applies the configured button type', () => {
    const fixture = TestBed.createComponent(ListItemHostComponent);
    fixture.componentInstance.interactive = true;
    fixture.componentInstance.type = 'reset';
    fixture.detectChanges();

    const item = getItem(fixture) as HTMLButtonElement;
    item.click();

    expect(item.getAttribute('type')).toBe('reset');
    expect(fixture.componentInstance.handlePressed).toHaveBeenCalledTimes(1);
  });

  it('renders as a link when an href is supplied and highlights the selected state', () => {
    const fixture = TestBed.createComponent(ListItemHostComponent);
    fixture.componentInstance.href = '/budgets/current';
    fixture.componentInstance.selected = true;
    fixture.detectChanges();

    const item = getItem(fixture);

    expect(item.tagName).toBe('A');
    expect(item.getAttribute('href')).toBe('/budgets/current');
    expect(item.getAttribute('aria-current')).toBe('page');
    expect(item.dataset.selected).toBe('true');
    expect(item.className).toContain('bg-mnl-accent/10');
    expect(item.className).toContain('border-mnl-accent/25');
  });

  it('applies rel=\"noopener noreferrer\" for new-tab links by default', () => {
    const fixture = TestBed.createComponent(ListItemHostComponent);
    fixture.componentInstance.href = '/budgets/current';
    fixture.componentInstance.target = '_blank';
    fixture.detectChanges();

    const item = getItem(fixture);
    expect(item.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('preserves an explicit rel value for links', () => {
    const fixture = TestBed.createComponent(ListItemHostComponent);
    fixture.componentInstance.href = '/budgets/current';
    fixture.componentInstance.rel = 'external';
    fixture.detectChanges();

    expect(getItem(fixture).getAttribute('rel')).toBe('external');
  });
});

function getBody(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-list-item-body"]') as HTMLElement;
}

function getItem(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-list-item"]') as HTMLElement;
}

function getLeading(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-list-item-leading"]',
  ) as HTMLElement;
}

function getTrailing(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-list-item-trailing"]',
  ) as HTMLElement;
}
