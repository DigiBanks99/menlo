import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlAvatarComponent, MnlAvatarSize } from './avatar.component';

const sampleAvatarSvg =
  'data:image/svg+xml;utf8,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 56 56%22%3E%3Crect width=%2256%22 height=%2256%22 rx=%2228%22 fill=%22%23ea76cb%22/%3E%3Ctext x=%2250%25%22 y=%2253%25%22 font-family=%22Arial%22 font-size=%2222%22 fill=%22%2311111b%22 text-anchor=%22middle%22 dominant-baseline=%22middle%22%3EWB%3C/text%3E%3C/svg%3E';

@Component({
  standalone: true,
  imports: [MnlAvatarComponent],
  template: ` <mnl-avatar [alt]="alt" [fallback]="fallback" [size]="size" [src]="src" /> `,
})
class AvatarHostComponent {
  alt = 'Wilco Boshoff';
  fallback = 'Wilco Boshoff';
  size: MnlAvatarSize = 'md';
  src = sampleAvatarSvg;
}

describe('MnlAvatarComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AvatarHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders an image when a source is provided', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.detectChanges();

    const avatar = getAvatar(fixture);
    const image = getImage(fixture);

    expect(avatar.getAttribute('data-state')).toBe('image');
    expect(avatar.getAttribute('data-size')).toBe('md');
    expect(image).not.toBeNull();
    expect(image?.getAttribute('alt')).toBe('Wilco Boshoff');
  });

  it('renders fallback initials when no image source is available', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.componentInstance.src = '';
    fixture.detectChanges();

    const avatar = getAvatar(fixture);
    const fallback = getFallback(fixture);

    expect(avatar.getAttribute('data-state')).toBe('fallback');
    expect(avatar.getAttribute('role')).toBe('img');
    expect(avatar.getAttribute('aria-label')).toBe('Wilco Boshoff');
    expect(fallback?.textContent?.trim()).toBe('WB');
  });

  it('falls back to initials when the image fails to load', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.detectChanges();

    getImage(fixture)?.dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(getAvatar(fixture).getAttribute('data-state')).toBe('fallback');
    expect(getFallback(fixture)?.textContent?.trim()).toBe('WB');
  });

  it('renders the default user icon when neither an image nor fallback text is available', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.componentInstance.src = '';
    fixture.componentInstance.fallback = '';
    fixture.detectChanges();

    expect(getAvatar(fixture).getAttribute('data-state')).toBe('fallback');
    expect(getIcon(fixture)).not.toBeNull();
  });

  it.each([
    ['sm', 'size-8', 'size-4'],
    ['lg', 'size-14', 'size-7'],
  ] satisfies readonly [MnlAvatarSize, string, string][])(
    'applies the %s size classes to the avatar shell and fallback icon',
    (size, avatarClass, iconClass) => {
      const fixture = TestBed.createComponent(AvatarHostComponent);
      fixture.componentInstance.size = size;
      fixture.componentInstance.src = '';
      fixture.componentInstance.fallback = '';
      fixture.detectChanges();

      expect(getAvatar(fixture).className).toContain(avatarClass);
      expect(getIconSvg(fixture)?.getAttribute('class')).toContain(iconClass);
    },
  );

  it('normalizes special characters in single-word fallback text and uses the computed initials label', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.componentInstance.alt = '';
    fixture.componentInstance.src = '';
    fixture.componentInstance.fallback = '@Alice!';
    fixture.detectChanges();

    expect(getAvatar(fixture).getAttribute('aria-label')).toBe('AL');
    expect(getFallback(fixture)?.textContent?.trim()).toBe('AL');
  });

  it('falls back to the default avatar label when no usable alt or fallback text exists', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.componentInstance.alt = ' ';
    fixture.componentInstance.src = '';
    fixture.componentInstance.fallback = '!!!';
    fixture.detectChanges();

    expect(getAvatar(fixture).getAttribute('aria-label')).toBe('Avatar');
    expect(getIcon(fixture)).not.toBeNull();
  });

  it('resets image failure state when the source changes', () => {
    const fixture = TestBed.createComponent(MnlAvatarComponent);
    fixture.componentRef.setInput('alt', 'Wilco Boshoff');
    fixture.componentRef.setInput('fallback', 'Wilco Boshoff');
    fixture.componentRef.setInput('src', sampleAvatarSvg);
    fixture.detectChanges();

    getImage(fixture)?.dispatchEvent(new Event('error'));
    fixture.detectChanges();

    fixture.componentRef.setInput(
      'src',
      'data:image/svg+xml;utf8,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 56 56%22%3E%3Crect width=%2256%22 height=%2256%22 rx=%2228%22 fill=%22%238839ef%22/%3E%3C/svg%3E',
    );
    fixture.detectChanges();

    expect(getAvatar(fixture).getAttribute('data-state')).toBe('image');
    expect(getImage(fixture)?.getAttribute('src')).toContain('%238839ef');
  });

  it('keeps the image state when the image load event succeeds', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.detectChanges();

    getImage(fixture)?.dispatchEvent(new Event('load'));
    fixture.detectChanges();

    expect(getAvatar(fixture).getAttribute('data-state')).toBe('image');
    expect(getImage(fixture)?.getAttribute('alt')).toBe('Wilco Boshoff');
  });

  it('uses the computed fallback text as image alt text when alt is blank', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.componentInstance.alt = ' ';
    fixture.componentInstance.fallback = 'Alice Liddell';
    fixture.detectChanges();

    expect(getImage(fixture)?.getAttribute('alt')).toBe('AL');
  });

  it('falls back to the default image alt text when no usable alt or fallback text exists', () => {
    const fixture = TestBed.createComponent(AvatarHostComponent);
    fixture.componentInstance.alt = ' ';
    fixture.componentInstance.fallback = '!!!';
    fixture.detectChanges();

    expect(getImage(fixture)?.getAttribute('alt')).toBe('Avatar');
  });
});

function getAvatar(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-avatar"]') as HTMLElement;
}

function getFallback(fixture: { nativeElement: HTMLElement }): HTMLElement | null {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-avatar-fallback"]',
  ) as HTMLElement | null;
}

function getIcon(fixture: { nativeElement: HTMLElement }): HTMLElement | null {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-avatar-icon"]',
  ) as HTMLElement | null;
}

function getIconSvg(fixture: { nativeElement: HTMLElement }): SVGElement | null {
  return getIcon(fixture)?.querySelector('svg') as SVGElement | null;
}

function getImage(fixture: { nativeElement: HTMLElement }): HTMLImageElement | null {
  return fixture.nativeElement.querySelector(
    '[data-testid="mnl-avatar-image"]',
  ) as HTMLImageElement | null;
}
