# Onboarding API Reference

The first time someone signs in, Menlo creates their user (see [Authentication API](authentication-api.md#provisioning-on-first-sign-in)). Before they can use the app, they finish onboarding. Today onboarding has one task: create or join a household.

## Domain model

`OnboardingState` (`Menlo.Lib.Onboarding`) is an aggregate root, one per user, stored in `shared.onboarding_states` through `IOnboardingContext`.

- `UserId`: the user it belongs to.
- `CompletedTasks`: the `OnboardingTask`s done so far, each with its `TaskType`, completion time and optional metadata.
- `HasSelectedHousehold`: true once the `SelectedHousehold` task is complete.
- `IsFullyOnboarded`: true once every required task is complete. The only required task is `SelectedHousehold`.
- `CompleteTask(taskType, metadata)`: records a task and raises `OnboardingTaskCompletedEvent`. It raises `OnboardingCompletedEvent` when the last required task completes. Completing a task twice does nothing.

`OnboardingTaskType` has one value, `SelectedHousehold`. To add a task, add a value, add it to `RequiredTaskTypes`, and add an endpoint that completes it.

`Household` (`Menlo.Lib.Auth.Entities`) is auditable and soft-deletable, stored in `shared.households` through `IHouseholdContext`. It has an `Id` and a `Name`.

## Endpoints

The household endpoints are under `/api/households` and require `RequireAuthenticated`. They don't require onboarding to be complete, because people use them during onboarding. Like every `/api` mutation, `POST` requests need the anti-forgery header (see [Anti-forgery](authentication-api.md#anti-forgery)).

### `GET /api/households`

Lists every household, ordered by name.

```json
{
  "households": [{ "id": "550e8400-e29b-41d4-a716-446655440000", "name": "Example Household" }]
}
```

### `POST /api/households`

Creates a household, assigns the current user to it, and completes the `SelectedHousehold` task, with the household ID as metadata.

Request:

```json
{ "name": "Example Household" }
```

| Status            | When                                                                         |
| ----------------- | ---------------------------------------------------------------------------- |
| `201 Created`     | Created. Body: `{ "id": "...", "name": "..." }`, with a `Location` header. |
| `400 Bad Request` | The name, trimmed, is empty or longer than 100 characters.                   |
| `401`             | Not signed in, or no provisioned user.                                       |
| `409 Conflict`    | A household with the same name already exists, ignoring case.               |

### `POST /api/households/{id}/join`

Assigns the current user to an existing household and completes the `SelectedHousehold` task. No request body.

| Status           | When                                     |
| ---------------- | ---------------------------------------- |
| `204 No Content` | Joined.                                  |
| `401`            | Not signed in, or no provisioned user.   |
| `404 Not Found`  | No household with that ID.               |

`id` must be a GUID; any other value doesn't match the route, so it returns `404`.

Users provisioned before onboarding state existed have no `OnboardingState` row. Creating or joining a household creates the row for them.

### `GET /auth/user`

Returns the current user's profile. The app uses it for both sign-in state and onboarding state.

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "person@example.com",
  "displayName": "Example Person",
  "roles": ["Menlo.User"],
  "onboarding": { "isComplete": false, "pendingTasks": ["SelectHousehold"] }
}
```

- `id`, `email` and `displayName` come from the stored user. If the user can't be found or loaded, they fall back to the token's claims.
- `roles` are the token's role claims.
- `onboarding.isComplete` is true once the user has selected a household. Until then, `pendingTasks` is `["SelectHousehold"]`; afterwards it is empty. If onboarding state can't be read, the endpoint reports onboarding as incomplete.
- Returns `401` when not signed in.

## In the app

The Angular routes (`menlo-app/src/app/app.routes.ts`) enforce onboarding:

- `/onboarding` uses `authGuard` and `noReenterOnboardingGuard`, which sends someone who has finished onboarding to `/`.
- Every other signed-in route uses `authGuard` and `onboardingGuard`. `onboardingGuard` sends someone who hasn't finished onboarding to `/onboarding?returnUrl=...`.
- After creating or joining a household, `OnboardingService` reloads `/auth/user` so the guards see the change.

The API doesn't block other `/api` endpoints for people who haven't finished onboarding. Budget endpoints fail with `UserNotAssignedToHouseholdError` until the user has a household.
