import { describe, expect, it } from 'vitest';

import { MenloRoles, OnboardingInfo, UserProfile } from './auth.models';

describe('AuthModels', () => {
  it('should expose expected Menlo roles', () => {
    expect(MenloRoles.Admin).toBe('Menlo.Admin');
    expect(MenloRoles.User).toBe('Menlo.User');
    expect(MenloRoles.Reader).toBe('Menlo.Reader');
  });

  it('should allow constructing a user profile', () => {
    const profile: UserProfile = {
      id: 'id',
      email: 'email@example.com',
      displayName: 'Name',
      roles: [MenloRoles.User],
    };

    expect(profile.roles).toEqual(['Menlo.User']);
  });

  it('should create incomplete onboarding state', () => {
    const info: OnboardingInfo = {
      isComplete: false,
      pendingTasks: ['SelectHousehold'],
    };

    expect(info.isComplete).toBe(false);
    expect(info.pendingTasks).toContain('SelectHousehold');
  });

  it('should create complete onboarding state', () => {
    const info: OnboardingInfo = {
      isComplete: true,
      pendingTasks: [],
    };

    expect(info.isComplete).toBe(true);
    expect(info.pendingTasks).toHaveLength(0);
  });
});
