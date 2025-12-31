import { describe, expect, it } from 'vitest';

import * as auth from './index';

describe('core/auth barrel export', () => {
  it('GivenIndexExports_WhenImported_ThenExportsExpectedSymbols', () => {
    expect(auth.AuthService).toBeDefined();
    expect(auth.authInterceptor).toBeDefined();
    expect(auth.authGuard).toBeDefined();
    expect(auth.roleGuard).toBeDefined();
    expect(auth.MenloRoles).toBeDefined();
  });
});
