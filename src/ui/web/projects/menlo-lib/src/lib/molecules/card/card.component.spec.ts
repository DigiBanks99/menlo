import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlCardComponent, type MnlCardPadding } from './card.component';

@Component({
  standalone: true,
  imports: [MnlCardComponent],
  template: `
    <mnl-card [interactive]="interactive" [padding]="padding">
      <div mnlCardHeader>Budget summary</div>
      <p>Track spending, savings, and upcoming commitments in one place.</p>
      <button mnlCardFooter type="button">Review</button>
    </mnl-card>
  `,
})
class CardHostComponent {
  interactive = false;
  padding: MnlCardPadding = 'md';
}

describe('MnlCardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('projects header, body, and footer content into the correct slots', () => {
    const fixture = TestBed.createComponent(CardHostComponent);
    fixture.detectChanges();

    expect(getHeader(fixture).textContent?.trim()).toBe('Budget summary');
    expect(getBody(fixture).textContent).toContain(
      'Track spending, savings, and upcoming commitments in one place.',
    );
    expect(getFooter(fixture).textContent?.trim()).toBe('Review');
  });

  it('applies the interactive hover treatment when requested', () => {
    const fixture = TestBed.createComponent(CardHostComponent);
    fixture.componentInstance.interactive = true;
    fixture.detectChanges();

    const card = getCard(fixture);

    expect(card.dataset.interactive).toBe('true');
    expect(card.className).toContain('hover:-translate-y-0.5');
    expect(card.className).toContain('hover:shadow-md');
  });

  it('exposes the configured padding token on the card root', () => {
    const fixture = TestBed.createComponent(CardHostComponent);
    fixture.componentInstance.padding = 'lg';
    fixture.detectChanges();

    expect(getCard(fixture).dataset.padding).toBe('lg');
  });
});

function getBody(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-card-body"]') as HTMLElement;
}

function getCard(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-card"]') as HTMLElement;
}

function getFooter(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-card-footer"]') as HTMLElement;
}

function getHeader(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-card-header"]') as HTMLElement;
}
