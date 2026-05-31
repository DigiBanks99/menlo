import { provideZonelessChangeDetection, signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HouseholdDto } from 'data-access-menlo-api';
import { OnboardingService } from '../../core/onboarding/onboarding.service';
import { OnboardingComponent } from './onboarding.component';

describe('OnboardingComponent', () => {
  let onboardingService: {
    $households: WritableSignal<HouseholdDto[]>;
    $isLoading: WritableSignal<boolean>;
    $error: WritableSignal<string | null>;
    loadHouseholds: ReturnType<typeof vi.fn>;
    joinHousehold: ReturnType<typeof vi.fn>;
    createHousehold: ReturnType<typeof vi.fn>;
    clearError: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    onboardingService = {
      $households: signal([{ id: 'household-1', name: 'Family Home' }]),
      $isLoading: signal(false),
      $error: signal<string | null>(null),
      loadHouseholds: vi.fn(),
      joinHousehold: vi.fn(),
      createHousehold: vi.fn(),
      clearError: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [OnboardingComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: OnboardingService, useValue: onboardingService },
      ],
    }).compileComponents();
  });

  it('should load households on init and render the available options', () => {
    const fixture = TestBed.createComponent(OnboardingComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(onboardingService.loadHouseholds).toHaveBeenCalledOnce();
    expect(compiled.querySelector('[data-testid="onboarding-title"]')?.textContent).toContain(
      'Welcome to Menlo',
    );
    expect(compiled.querySelector('[data-testid="household-list"]')?.textContent).toContain(
      'Family Home',
    );
  });

  it('should call the onboarding service when joining a household', () => {
    const fixture = TestBed.createComponent(OnboardingComponent);
    fixture.detectChanges();

    const joinButton = fixture.debugElement.query(By.css('[data-testid="join-household-household-1"]'));
    joinButton.nativeElement.click();

    expect(onboardingService.joinHousehold).toHaveBeenCalledWith('household-1');
  });

  it('should validate the create form before submission', () => {
    const fixture = TestBed.createComponent(OnboardingComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.nameControl.setValue('');
    component.createHousehold();
    fixture.detectChanges();

    expect(onboardingService.createHousehold).not.toHaveBeenCalled();
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="household-name-error"]')
        ?.textContent,
    ).toContain('Household name is required');
  });

  it('should submit a trimmed household name', () => {
    const fixture = TestBed.createComponent(OnboardingComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.nameControl.setValue('  New Family  ');
    component.createHousehold();

    expect(onboardingService.createHousehold).toHaveBeenCalledWith('New Family');
  });

  it('should reflect the loading state in the UI', () => {
    onboardingService.$isLoading.set(true);

    const fixture = TestBed.createComponent(OnboardingComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const createButton = compiled.querySelector('[data-testid="create-household-button"]') as HTMLButtonElement;
    const joinButton = compiled.querySelector('[data-testid="join-household-household-1"]') as HTMLButtonElement;

    expect(compiled.querySelector('[data-testid="loading-households"]')?.textContent).toContain(
      'Loading households...',
    );
    expect(createButton.disabled).toBe(true);
    expect(createButton.textContent).toContain('Creating...');
    expect(joinButton.disabled).toBe(true);
  });

  it('should display and dismiss errors', () => {
    onboardingService.$error.set('Network error');

    const fixture = TestBed.createComponent(OnboardingComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="onboarding-error"]')?.textContent).toContain(
      'Network error',
    );

    const dismissButton = fixture.debugElement.query(By.css('[data-testid="onboarding-error"] button'));
    dismissButton.nativeElement.click();

    expect(onboardingService.clearError).toHaveBeenCalledOnce();
  });
});
