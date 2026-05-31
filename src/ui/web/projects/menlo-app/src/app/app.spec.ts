import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { App } from './app';

@Component({
  standalone: true,
  template: '<p>Home page</p>',
})
class HomeRouteComponent {}

@Component({
  standalone: true,
  template: '<p>Sign in page</p>',
})
class SignInRouteComponent {}

@Component({
  standalone: true,
  template: '<p>Onboarding page</p>',
})
class OnboardingRouteComponent {}

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([
          { path: '', component: HomeRouteComponent },
          { path: 'sign-in', component: SignInRouteComponent },
          { path: 'onboarding', component: OnboardingRouteComponent },
        ]),
        provideZonelessChangeDetection(),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the page shell for authenticated routes', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('[data-testid="mnl-page-shell"]')).toBeTruthy();
  });

  it('should hide the page shell on the sign-in route', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/sign-in');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('[data-testid="mnl-page-shell"]')).toBeNull();
    expect(compiled.textContent).toContain('Sign in page');
  });

  it('should hide the page shell on the onboarding route', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/onboarding');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('[data-testid="mnl-page-shell"]')).toBeNull();
    expect(compiled.textContent).toContain('Onboarding page');
  });
});
