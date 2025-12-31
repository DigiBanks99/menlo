export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export const MenloRoles = {
  Admin: 'Menlo.Admin',
  User: 'Menlo.User',
  Reader: 'Menlo.Reader',
} as const;
