import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../core/auth/auth.service';
import { SignInComponent } from './sign-in.component';

describe('SignInComponent', () => {
  let authService: {
    login: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authService = {
      login: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [SignInComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: (key: string) => (key === 'returnUrl' ? '/budgets/2026' : null),
              },
            },
          },
        },
      ],
    }).compileComponents();
  });

  it('should render the microsoft sign-in button', () => {
    const fixture = TestBed.createComponent(SignInComponent);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('[data-testid="sign-in-button"]');
    expect(button?.textContent?.trim()).toBe('Sign in with Microsoft');
  });

  it('should send the requested return url to the auth service', () => {
    const fixture = TestBed.createComponent(SignInComponent);
    fixture.detectChanges();

    fixture.componentInstance.signIn();

    expect(authService.login).toHaveBeenCalledWith('/budgets/2026');
  });

  it('should fall back to home when no return url is provided', async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [SignInComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: () => null,
              },
            },
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(SignInComponent);
    fixture.detectChanges();

    fixture.componentInstance.signIn();

    expect(authService.login).toHaveBeenCalledWith('/');
  });
});
