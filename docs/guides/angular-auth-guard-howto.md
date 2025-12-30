# How-to: Add a New Angular Role Guard

This guide explains how to create a new Route Guard in Angular to protect routes based on user roles.

## Prerequisites

- An `AuthService` that provides the current user's state and roles.
- Angular 16+ (using Functional Guards).

## Step 1: Create the Guard

Create a new file for your guard, e.g., `src/app/core/auth/guards/admin.guard.ts`.

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth.service'; // Adjust path as needed
import { map } from 'rxjs/operators';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.user$.pipe(
    map(user => {
      // Check if user is logged in and has the Admin role
      if (user && user.roles.includes('Menlo.Admin')) {
        return true;
      }

      // Redirect to home or login if not authorized
      return router.createUrlTree(['/']);
    })
  );
};
```

## Step 2: Register the Guard in Routes

Open your routing configuration (e.g., `app.routes.ts`) and apply the guard to the desired route.

```typescript
import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/guards/admin.guard';

export const routes: Routes = [
  {
    path: 'admin',
    loadComponent: () => import('./admin/admin.component').then(m => m.AdminComponent),
    canActivate: [adminGuard] // <--- Apply the guard here
  },
  // ... other routes
];
```

## Step 3: Handle Unauthorized Access

Ensure your guard handles the "false" case gracefully, typically by returning a `UrlTree` to redirect the user, as shown in the example above.

## Summary

You have created a functional route guard that checks for a specific role and applied it to a route.
