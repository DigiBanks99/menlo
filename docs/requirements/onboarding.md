# User Onboarding Flow

## Table of Contents

- [User Onboarding Flow](#user-onboarding-flow)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Problem Statement](#problem-statement)
  - [Solution Architecture](#solution-architecture)
    - [Domain Model](#domain-model)
    - [Backend Provisioning](#backend-provisioning)
    - [Frontend State Management](#frontend-state-management)
    - [Navigation Guards](#navigation-guards)
  - [API Specification](#api-specification)
    - [Auth Response Enhancement](#auth-response-enhancement)
    - [Onboarding Endpoints](#onboarding-endpoints)
    - [Error Handling](#error-handling)
  - [Frontend Implementation](#frontend-implementation)
    - [Angular State Service](#angular-state-service)
    - [Route Guards](#route-guards)
    - [Onboarding Component](#onboarding-component)
  - [User Flow Diagram](#user-flow-diagram)
  - [Extension Points (Future Onboarding Tasks)](#extension-points-future-onboarding-tasks)
    - [Task Types](#task-types)
    - [Adding New Onboarding Tasks](#adding-new-onboarding-tasks)
  - [Testing Strategy](#testing-strategy)
  - [Success Criteria](#success-criteria)

## Overview

This document defines the mandatory user onboarding flow that gates new users from accessing the Menlo application until they have successfully selected or created a household.

**Scope:** Applies to both **Aspire local development** and **production deployment** (CloudFlare tunnel → local server).

## Problem Statement

When a user authenticates via Azure AD OIDC for the first time:

1. ✅ Azure AD authentication succeeds
2. ✅ User is authenticated (has valid session cookie and roles)
3. ❌ No `User` record exists in `shared.users` table
4. ❌ Protected API endpoints return **401 Unauthorized** (from inside handler, not auth middleware)
5. ❌ User cannot use the app despite being authenticated

**Root cause:** `UserContextProvider.GetUserContextAsync()` requires both a User record *and* a Household context. First-time users fail at the User lookup step, then would fail at the Household lookup.

## Solution Architecture

### Domain Model

#### `UserOnboarding` Aggregate (New Entity)

Tracks onboarding completion per user, with a collection of completed tasks.

```csharp
// Menlo.Domain/Onboarding/UserOnboarding.cs
public class UserOnboarding : AggregateRoot
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    
    /// <summary>
    /// Completed onboarding tasks (ValueObject collection).
    /// Check <c>CompletedTasks.Count == AllRequiredTasks.Count</c> to determine full completion.
    /// For MVP, household selection gates navigation; other tasks may be added later.
    /// </summary>
    public ICollection<OnboardingTask> CompletedTasks { get; private set; } = new List<OnboardingTask>();
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public UserOnboarding(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Mark a specific onboarding task as completed.
    /// Idempotent — adding the same task twice does not create duplicates.
    /// </summary>
    public void CompleteTask(OnboardingTaskType taskType, Dictionary<string, object>? metadata = null)
    {
        if (CompletedTasks.Any(t => t.TaskType == taskType))
            return; // Already completed

        CompletedTasks.Add(new OnboardingTask(taskType, DateTime.UtcNow, metadata));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// True if all required onboarding tasks are complete.
    /// Used by backend authorization policies and frontend guards.
    /// </summary>
    public bool IsFullyOnboarded => CompletedTasks.Count >= GetRequiredTaskCount();

    /// <summary>
    /// True if household selection task is completed.
    /// MVP gates navigation to this; future tasks may have different completion criteria.
    /// </summary>
    public bool HasSelectedHousehold => CompletedTasks.Any(t => t.TaskType == OnboardingTaskType.SelectedHousehold);

    private static int GetRequiredTaskCount() => Enum.GetValues(typeof(OnboardingTaskType))
        .Cast<OnboardingTaskType>()
        .Count(t => IsTaskRequired(t));

    private static bool IsTaskRequired(OnboardingTaskType taskType) => taskType switch
    {
        OnboardingTaskType.SelectedHousehold => true,
        // Future: OnboardingTaskType.SetupProfile => true, etc.
        _ => false,
    };
}
```

#### `OnboardingTask` ValueObject

```csharp
// Menlo.Domain/Onboarding/OnboardingTask.cs
public sealed record OnboardingTask
{
    public OnboardingTaskType TaskType { get; init; }
    public DateTime CompletedAt { get; init; }
    
    /// <summary>
    /// Optional metadata (e.g., { "householdId": "xyz" } for SelectedHousehold task).
    /// Dictionary<string, object> for JSON serialization flexibility.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    public OnboardingTask(OnboardingTaskType taskType, DateTime completedAt, Dictionary<string, object>? metadata = null)
    {
        TaskType = taskType;
        CompletedAt = completedAt;
        Metadata = metadata;
    }
}

public enum OnboardingTaskType
{
    SelectedHousehold = 1,
    // Future: SetupProfile = 2, ConfigurePreferences = 3, etc.
}
```

#### Entity Type Configuration

```csharp
// Menlo.Persistence/Configurations/UserOnboardingConfiguration.cs
public class UserOnboardingConfiguration : IEntityTypeConfiguration<UserOnboarding>
{
    public void Configure(EntityTypeBuilder<UserOnboarding> builder)
    {
        builder.HasKey(u => u.UserId);
        
        builder.HasOne(u => u.User)
            .WithOne()
            .HasForeignKey<UserOnboarding>(u => u.UserId);

        builder.OwnsMany(
            u => u.CompletedTasks,
            ownedBuilder =>
            {
                ownedBuilder.ToJson(); // EF Core 8+ JSON column support
                ownedBuilder.Property(t => t.TaskType).HasConversion<int>();
                ownedBuilder.Property(t => t.Metadata).HasConversion(
                    dict => JsonConvert.SerializeObject(dict),
                    json => JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new());
            });

        builder.Property(u => u.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(u => u.UpdatedAt).HasColumnType("timestamp with time zone");
    }
}
```

### Backend Provisioning

#### 1. User Auto-Provisioning (OIDC Token Validation)

**When:** OIDC `OnTokenValidated` event fires (after token validation succeeds)

**What happens:**

1. Extract `external_id` (e.g., Azure AD ObjectId) from token claims
2. Query `shared.users` for matching `ExternalId`
3. If not found:
   - Create new `User` record with `ExternalId`, `Email`, `Name` from claims
   - Create new `UserOnboarding` record linked to the user (with `CompletedTasks = []`)
4. If found: skip (user already provisioned)

**Code location:** `src/api/Menlo.Api/Auth/OidcEvents.cs` (new file)

```csharp
public class MenloOidcEvents : OpenIdConnectEvents
{
    private readonly IServiceProvider _serviceProvider;

    public MenloOidcEvents(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task OnTokenValidated(TokenValidatedContext context)
    {
        var externalId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Principal?.FindFirst("oid")?.Value;
        
        if (string.IsNullOrEmpty(externalId))
            return;

        using var scope = _serviceProvider.CreateAsyncScope();
        var userContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        var existingUser = await userContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId);

        if (existingUser == null)
        {
            // Auto-provision user
            var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@tenant.onmicrosoft.com";
            var name = context.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? email;

            var newUser = new User
            {
                ExternalId = externalId,
                Email = email,
                FullName = name,
                CreatedAt = DateTime.UtcNow,
            };

            userContext.Users.Add(newUser);
            await userContext.SaveChangesAsync();

            // Auto-create empty UserOnboarding record
            var onboarding = new UserOnboarding(newUser.Id);
            userContext.Set<UserOnboarding>().Add(onboarding);
            await userContext.SaveChangesAsync();
        }

        await base.OnTokenValidated(context);
    }
}
```

**Registration in Startup:**

```csharp
// In Program.cs
builder.Services.AddAuthentication(options =>
{
    // ... existing config
})
.AddOpenIdConnect(options =>
{
    options.Events = new MenloOidcEvents(builder.Services.BuildServiceProvider());
    // ... other options
});
```

#### 2. Authentication Response Enhancement

**Endpoint:** `GET /auth/user` (or wherever login response is returned)

**Change:** Include `IsOnboardingCompleted` in response DTO

```csharp
public record AuthUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string[] Roles,
    bool IsOnboardingCompleted);  // ← NEW

// In the endpoint handler:
var userOnboarding = await dbContext.Set<UserOnboarding>()
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.UserId == user.Id);

var response = new AuthUserResponse(
    user.Id,
    user.Email,
    user.FullName,
    roles,
    userOnboarding?.HasSelectedHousehold ?? false);  // MVP gate

return Ok(response);
```

#### 3. Protected Endpoints Authorization

**Approach:** Custom ASP.NET Core authorization policy

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OnboardingComplete", policy =>
        policy.Requirements.Add(new OnboardingCompleteRequirement()));

builder.Services.AddScoped<IAuthorizationHandler, OnboardingCompleteHandler>();
```

```csharp
// Menlo.Api/Auth/Requirements/OnboardingCompleteHandler.cs
public class OnboardingCompleteHandler : AuthorizationHandler<OnboardingCompleteRequirement>
{
    private readonly MenloDbContext _dbContext;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OnboardingCompleteRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            context.Fail();
            return;
        }

        var userOnboarding = await _dbContext.Set<UserOnboarding>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (userOnboarding?.HasSelectedHousehold ?? false)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
```

**Apply to protected routes:**

```csharp
app.MapGroup("/api/budgets")
    .RequireAuthorization("OnboardingComplete")
    // ... endpoint definitions
```

### Frontend State Management

#### Onboarding State Service

```typescript
// projects/menlo-app/src/app/core/services/onboarding-state.service.ts
import { Injectable } from '@angular/core';

const ONBOARDING_STATE_KEY = 'menlo.onboarding_state';

export interface OnboardingState {
  isOnboardingCompleted: boolean;
  lastSyncAt: number; // timestamp
}

@Injectable({ providedIn: 'root' })
export class OnboardingStateService {
  private readonly storageKey = ONBOARDING_STATE_KEY;

  /** Initialize state from localStorage or auth response. */
  initializeFromAuthResponse(isOnboardingCompleted: boolean): void {
    this.writeState({ isOnboardingCompleted, lastSyncAt: Date.now() });
  }

  /** Mark household selection as complete. */
  markHouseholdSelected(): void {
    this.writeState({ isOnboardingCompleted: true, lastSyncAt: Date.now() });
  }

  /** Check if onboarding is complete (reads localStorage). */
  isComplete(): boolean {
    const state = this.readState();
    return state?.isOnboardingCompleted ?? false;
  }

  /** Clear on logout. */
  clear(): void {
    localStorage.removeItem(this.storageKey);
  }

  private readState(): OnboardingState | null {
    try {
      const json = localStorage.getItem(this.storageKey);
      return json ? JSON.parse(json) : null;
    } catch {
      return null;
    }
  }

  private writeState(state: OnboardingState): void {
    try {
      localStorage.setItem(this.storageKey, JSON.stringify(state));
    } catch (err) {
      console.error('Failed to persist onboarding state', err);
    }
  }
}
```

### Navigation Guards

#### Guard 1: Block Incomplete Users from Protected Routes

```typescript
// projects/menlo-app/src/app/core/guards/require-onboarding-complete.guard.ts
import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { OnboardingStateService } from '../services/onboarding-state.service';

export const requireOnboardingCompleteGuard: CanActivateFn = (route, state) => {
  const onboarding = inject(OnboardingStateService);
  const router = inject(Router);

  if (onboarding.isComplete()) {
    return true;
  }

  return router.parseUrl('/onboard');
};
```

#### Guard 2: Prevent Re-Entering Onboarding (Inverse Guard)

```typescript
// projects/menlo-app/src/app/features/onboarding/guards/no-reenter-onboarding.guard.ts
import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { OnboardingStateService } from '../services/onboarding-state.service';

export const noReenterOnboardingGuard: CanActivateFn = (route, state) => {
  const onboarding = inject(OnboardingStateService);
  const router = inject(Router);

  if (onboarding.isComplete()) {
    return router.parseUrl('/home'); // redirect away
  }

  return true;
};
```

#### Route Configuration

```typescript
// projects/menlo-app/src/app/app.routes.ts
export const routes: Routes = [
  {
    path: 'onboard',
    component: OnboardingComponent,
    canActivate: [noReenterOnboardingGuard],
  },
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [requireOnboardingCompleteGuard],
  },
  {
    path: 'budgets',
    component: BudgetsComponent,
    canActivate: [requireOnboardingCompleteGuard],
  },
  // ... other routes
];
```

## API Specification

### Auth Response Enhancement

**Endpoint:** `GET /auth/user`

**Response:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "roles": ["Member"],
  "isOnboardingCompleted": false
}
```

**Change:** Add `isOnboardingCompleted` field (boolean).

### Onboarding Endpoints

#### List Available Households

**Endpoint:** `GET /api/onboarding/households`

**Response:**

```json
{
  "households": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Primary Residence",
      "createdAt": "2026-05-20T10:00:00Z"
    }
  ]
}
```

#### Create Household and Complete Onboarding

**Endpoint:** `POST /api/onboarding/households`

**Request:**

```json
{
  "name": "My Household",
  "description": "Primary residence"
}
```

**Response:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "name": "My Household",
  "createdAt": "2026-05-31T10:00:00Z"
}
```

**Side effect:** Marks `UserOnboarding.CompleteTask(OnboardingTaskType.SelectedHousehold, { householdId })`.

#### Select Existing Household and Complete Onboarding

**Endpoint:** `POST /api/onboarding/households/{householdId}/select`

**Response:**

```json
{
  "householdId": "550e8400-e29b-41d4-a716-446655440001",
  "selectedAt": "2026-05-31T10:00:00Z"
}
```

**Side effect:** Marks `UserOnboarding.CompleteTask(OnboardingTaskType.SelectedHousehold, { householdId })`.

### Error Handling

| Status | Scenario | Response |
|--------|----------|----------|
| 401 | User not authenticated | Standard auth error |
| 403 | User has not selected household, trying to access protected endpoint | Include `isOnboardingCompleted: false` in error details |
| 409 | Household name already exists | Descriptive error message |
| 500 | Onboarding state creation failed | Generic error (log details server-side) |

## Frontend Implementation

### Angular State Service

See [Angular State Service](#angular-state-service) above.

### Route Guards

See [Navigation Guards](#navigation-guards) above.

### Onboarding Component

**Responsibilities:**

- Display available households
- Allow user to select an existing household
- Allow user to create a new household
- Call API endpoint to complete onboarding
- Update local state via `OnboardingStateService`
- Redirect to home on success

**Example structure:**

```typescript
@Component({
  selector: 'app-onboarding',
  templateUrl: './onboarding.component.html',
  styleUrls: ['./onboarding.component.css'],
})
export class OnboardingComponent implements OnInit {
  households$ = new BehaviorSubject<Household[]>([]);
  loading$ = new BehaviorSubject(false);
  error$ = new BehaviorSubject<string | null>(null);

  constructor(
    private onboardingService: OnboardingService,
    private onboardingState: OnboardingStateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadHouseholds();
  }

  loadHouseholds(): void {
    this.loading$.next(true);
    this.onboardingService.listHouseholds().subscribe({
      next: (households) => {
        this.households$.next(households);
        this.loading$.next(false);
      },
      error: (err) => {
        this.error$.next(err.message);
        this.loading$.next(false);
      },
    });
  }

  selectHousehold(householdId: string): void {
    this.loading$.next(true);
    this.onboardingService.selectHousehold(householdId).subscribe({
      next: () => {
        this.onboardingState.markHouseholdSelected();
        this.router.navigate(['/home']);
      },
      error: (err) => {
        this.error$.next(err.message);
        this.loading$.next(false);
      },
    });
  }

  createHousehold(name: string): void {
    this.loading$.next(true);
    this.onboardingService.createHousehold(name).subscribe({
      next: () => {
        this.onboardingState.markHouseholdSelected();
        this.router.navigate(['/home']);
      },
      error: (err) => {
        this.error$.next(err.message);
        this.loading$.next(false);
      },
    });
  }
}
```

## User Flow Diagram

```text
┌─────────────────────────────────────────┐
│ User Visits App (First Time)            │
└─────────────────────────┬───────────────┘
                          ↓
        ┌─────────────────────────────────┐
        │ Redirected to Azure AD Sign In   │
        └──────────────┬────────────────────┘
                       ↓
        ┌──────────────────────────────┐
        │ OIDC Token Validation        │
        │ OnTokenValidated Event       │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ Auto-Provision User Record   │
        │ Auto-Create UserOnboarding   │
        │ (CompletedTasks = [])        │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ Return Auth Response         │
        │ isOnboardingCompleted: false │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ Frontend: Cache in           │
        │ localStorage + state service │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ Guard Triggers:              │
        │ /home or /budgets → blocked  │
        │ Redirect to /onboard         │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ Onboarding Component Loads   │
        │ Displays:                    │
        │ • List of households         │
        │ • "Create new household" btn │
        └──────────┬───────────────────┘
                   ↓
            ┌──────┴────────┐
            ↓               ↓
    ┌──────────────┐  ┌──────────────┐
    │ Select       │  │ Create New   │
    │ Household    │  │ Household    │
    └──────┬───────┘  └──────┬───────┘
           │                  │
           └────────┬─────────┘
                    ↓
        ┌──────────────────────────────┐
        │ POST /api/onboarding/...     │
        │ Mark: SelectedHousehold      │
        │ task complete in backend     │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ Frontend: Update State       │
        │ isOnboardingCompleted: true  │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ Navigate to /home            │
        │ Guard: Passes ✓              │
        └──────────┬───────────────────┘
                   ↓
        ┌──────────────────────────────┐
        │ User Now Has Access to App   │
        └──────────────────────────────┘
```

## Extension Points (Future Onboarding Tasks)

### Task Types

The `OnboardingTaskType` enum can be extended with new tasks without breaking existing logic:

```csharp
public enum OnboardingTaskType
{
    SelectedHousehold = 1,        // MVP: Gates navigation
    SetupProfile = 2,             // Future: Name, photo, preferences
    ConfigurePreferences = 3,     // Future: Currency, budget mode, etc.
    ConnectBankAccount = 4,       // Future: Open banking integration
}
```

### Adding New Onboarding Tasks

To add a new onboarding task (e.g., `SetupProfile`):

1. **Backend:**
   - Add enum value to `OnboardingTaskType`
   - Create handler to complete the task (e.g., `POST /api/onboarding/profile`)
   - Update `UserOnboarding.GetRequiredTaskCount()` if task is required for full completion

2. **Frontend:**
   - Create component for the task (e.g., `ProfileSetupComponent`)
   - Add step to onboarding wizard/stepper
   - Update `OnboardingComponent` logic to progress through steps

3. **Completion Logic:**
   - If the new task is **required:** check `UserOnboarding.IsFullyOnboarded` in guard
   - If the new task is **optional:** add separate guard or feature flag

### Example: Adding `SetupProfile` Task

**Backend:**

```csharp
// Handler
public async Task<IResult> SetupProfile(SetupProfileRequest request)
{
    var user = GetCurrentUser();
    var onboarding = await _dbContext.Set<UserOnboarding>()
        .FirstAsync(u => u.UserId == user.Id);

    onboarding.CompleteTask(
        OnboardingTaskType.SetupProfile,
        metadata: new() { { "profilePhotoUrl", request.PhotoUrl } });

    await _dbContext.SaveChangesAsync();
    return Ok();
}

// Update required count
private static bool IsTaskRequired(OnboardingTaskType taskType) => taskType switch
{
    OnboardingTaskType.SelectedHousehold => true,
    OnboardingTaskType.SetupProfile => true,  // ← NEW
    _ => false,
};
```

**Frontend:**

```typescript
// Add to onboarding wizard
const onboardingSteps = [
  { task: OnboardingTaskType.SelectedHousehold, component: HouseholdSelectionComponent },
  { task: OnboardingTaskType.SetupProfile, component: ProfileSetupComponent },
];
```

## Testing Strategy

### Backend Tests

1. **Provisioning Tests:**
   - User does not exist → auto-provisioned on first token validation
   - User already exists → no duplicate record created
   - `UserOnboarding` record created with empty `CompletedTasks`

2. **Authorization Tests:**
   - Unauthenticated request → 401
   - Authenticated but `HasSelectedHousehold = false` → 403 on protected endpoints
   - Authenticated and `HasSelectedHousehold = true` → 200 OK

3. **Household Selection Tests:**
   - Select existing household → `UserOnboarding` updated, task marked complete
   - Create new household → household created, `UserOnboarding` updated
   - Duplicate household name → 409 Conflict

### Frontend Tests

1. **State Service Tests:**
   - `markHouseholdSelected()` persists to localStorage
   - `isComplete()` reads from localStorage
   - `clear()` removes all state

2. **Guard Tests:**
   - `requireOnboardingCompleteGuard`: true when `isComplete()` = true, false otherwise
   - `noReenterOnboardingGuard`: false when `isComplete()` = true, true otherwise

3. **Component Tests:**
   - Households list displays correctly
   - Create household button triggers creation flow
   - Selection completes onboarding and navigates to home

4. **Integration Tests:**
   - Login → onboarding component shown
   - Select household → redirected to home
   - Navigating to `/home` before household selection → redirected to `/onboard`

## Success Criteria

✅ User can sign in with Azure AD and is immediately shown the onboarding flow (no 401 errors)

✅ After selecting or creating a household, user can access all protected endpoints (budgets, planning, etc.)

✅ Onboarding state persists across page reloads (localStorage-backed)

✅ Navigating to protected routes without completing onboarding redirects to `/onboard`

✅ Already-onboarded users cannot re-enter the `/onboard` route

✅ Both Aspire (dev) and production deployments work identically

✅ Extension model is clear: adding new onboarding tasks requires minimal changes to core flow

✅ API returns `IsOnboardingCompleted` in auth response (no separate status check call needed)
