import { HttpRequest } from '@angular/common/http';
import { describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { authInterceptor } from './auth.interceptor';

describe('AuthInterceptor', () => {
  it('should add withCredentials for API requests', () => {
    const request = new HttpRequest('GET', `${environment.apiBaseUrl}/auth/user`);

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req.withCredentials).toBe(true);
      return undefined as never;
    });

    authInterceptor(request, next);

    expect(next).toHaveBeenCalledOnce();
  });

  it('should not add withCredentials for non-API requests', () => {
    const request = new HttpRequest('GET', 'https://example.com/somewhere');

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req.withCredentials).toBe(false);
      return undefined as never;
    });

    authInterceptor(request, next);

    expect(next).toHaveBeenCalledOnce();
  });
});
