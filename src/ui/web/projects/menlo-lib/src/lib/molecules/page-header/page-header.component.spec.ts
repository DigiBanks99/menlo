import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlPageHeaderComponent, mnlPageHeaderDefaultGradient } from './page-header.component';

@Component({
  standalone: true,
  imports: [MnlPageHeaderComponent],
  template: `
    <mnl-page-header [gradient]="gradient">
      <div mnlPageHeaderHero>
        <p class="eyebrow">Household overview</p>
        <h1>Stay ahead of monthly spending</h1>
      </div>
      <article class="card">Overlap content</article>
    </mnl-page-header>
  `,
})
class PageHeaderHostComponent {
  gradient = mnlPageHeaderDefaultGradient;
}

describe('MnlPageHeaderComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PageHeaderHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('applies the configured gradient background', () => {
    const fixture = TestBed.createComponent(PageHeaderHostComponent);
    fixture.componentInstance.gradient = 'linear-gradient(180deg, red 0%, blue 100%)';
    fixture.detectChanges();

    expect(getGradient(fixture).style.backgroundImage).toContain(
      'linear-gradient(180deg, red 0%, blue 100%)',
    );
  });

  it('uses the theme gradient tokens for light and dark mode', () => {
    const fixture = TestBed.createComponent(PageHeaderHostComponent);
    fixture.detectChanges();

    expect(getGradient(fixture).style.backgroundImage).toContain('var(--mnl-color-gradient-start)');
    expect(getGradient(fixture).style.backgroundImage).toContain('var(--mnl-color-gradient-end)');
  });

  it('provides an overlap container that can pull content below the gradient edge', () => {
    const fixture = TestBed.createComponent(PageHeaderHostComponent);
    fixture.detectChanges();

    expect(getOverlap(fixture).className).toContain('-mt-14');
    expect(getOverlap(fixture).textContent).toContain('Overlap content');
  });
});

function getGradient(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-page-header-gradient"]',
  ) as HTMLElement;
}

function getOverlap(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-page-header-overlap"]',
  ) as HTMLElement;
}
