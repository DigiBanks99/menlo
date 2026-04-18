import { InjectionToken } from '@angular/core';

/**
 * Injection token for the API base URL.
 * Defaults to an empty string so requests use the dev proxy by default.
 * In environments that require direct API access (e.g. dev with CORS), provide
 * the absolute API origin (e.g. 'https://localhost:7298').
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  factory: () => '',
});
