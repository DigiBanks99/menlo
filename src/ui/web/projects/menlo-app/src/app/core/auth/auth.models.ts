export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  onboarding?: OnboardingInfo;
}

export type OnboardingTaskType = 'SelectHousehold';

export interface OnboardingInfo {
  isComplete: boolean;
  pendingTasks: OnboardingTaskType[];
}

export const MenloRoles = {
  Admin: 'Menlo.Admin',
  User: 'Menlo.User',
  Reader: 'Menlo.Reader',
} as const;
