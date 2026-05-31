# Onboarding API Contract

This document defines the TypeScript types and HTTP API contract for the onboarding feature (issue #362).

## Phase 0: Type Definitions

### Onboarding Types

```typescript
// menlo-app/core/auth/auth.models.ts

export type OnboardingTaskType = 'SelectHousehold';

export interface OnboardingInfo {
  isComplete: boolean;
  pendingTasks: OnboardingTaskType[];
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  onboarding?: OnboardingInfo;  // Optional during rollout; required after full deployment
}
```

### Household Types

```typescript
// data-access-menlo-api/src/lib/models/

export interface HouseholdDto {
  id: string;
  name: string;
}

export interface CreateHouseholdRequest {
  name: string;
}
```

## API Endpoints

All endpoints require authentication (`Bearer` token from Azure AD OIDC).

### 1. List Available Households

**Endpoint:** `GET /api/households`

**Authentication:** Required (any authenticated user)

**Onboarding Gate:** None (must be accessible before onboarding is complete)

**Response (200 OK):**

```json
{
  "households": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Smith Family"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Johnson Family"
    }
  ]
}
```

**Error Responses:**

- `401 Unauthorized` — No valid authentication token
- `500 Internal Server Error` — Database or service error

---

### 2. Create Household

**Endpoint:** `POST /api/households`

**Authentication:** Required (any authenticated user)

**Onboarding Gate:** None (must be accessible before onboarding is complete)

**Request Body:**

```json
{
  "name": "My Family"
}
```

**Response (201 Created):**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "name": "My Family"
}
```

**Error Responses:**

- `400 Bad Request` — Invalid request (missing name, empty string, etc.)
- `401 Unauthorized` — No valid authentication token
- `409 Conflict` — Household with this name already exists for user
- `500 Internal Server Error` — Database or service error

---

### 3. Join/Select Household (Onboarding Completion)

**Endpoint:** `POST /api/households/{id}/join`

**Path Parameters:**

- `id` (string, UUID) — Household ID to join

**Authentication:** Required (any authenticated user)

**Onboarding Gate:** None (must be accessible before onboarding is complete)

**Request Body:** Empty (no body required)

### Response (204 No Content)

No response body on success.

**Error Responses:**

- `400 Bad Request` — Invalid household ID format
- `401 Unauthorized` — No valid authentication token
- `403 Forbidden` — Household does not exist or user is not authorized to join
- `404 Not Found` — Household ID not found
- `500 Internal Server Error` — Database or service error

**Side Effects:**

- User is assigned to the household
- `OnboardingState.CompleteTask(SelectHousehold)` is marked complete
- User's `UserProfile.onboarding.isComplete` becomes `true`

---

### 4. Get User Profile (Enhanced)

**Endpoint:** `GET /auth/user`

**Authentication:** Required (any authenticated user)

**Response (200 OK):**

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "displayName": "John Smith",
  "roles": ["User"],
  "onboarding": {
    "isComplete": false,
    "pendingTasks": ["SelectHousehold"]
  }
}
```

**Response (200 OK, after onboarding):**

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "displayName": "John Smith",
  "roles": ["User"],
  "onboarding": {
    "isComplete": true,
    "pendingTasks": []
  }
}
```

**Error Responses:**

- `401 Unauthorized` — No valid authentication token or token expired

---

## Frontend Integration Points

### 1. Route Guard

The `onboardingGuard` will use the `UserProfile.onboarding.isComplete` flag to:

- Allow access to `/onboarding` route **without** requiring `onboarding.isComplete`
- Redirect incomplete users to `/onboarding` from protected routes (budget, planning, etc.)
- Allow access to protected routes **only if** `onboarding.isComplete === true`

### 2. State Persistence

The `OnboardingService` will:

- Cache `UserProfile.onboarding` in local state after login
- Call `/auth/user` to refresh onboarding state after joining/creating household
- Persist completion state across page refresh (cached from auth response)

### 3. Error Handling

- HTTP `401` → redirect to `/sign-in` (already authenticated, but session expired)
- HTTP `403` → display error "You are not authorized to join this household"
- HTTP `404` → display error "Household not found"
- HTTP `409` → display error "A household with this name already exists"

---

## Validation Rules

### Household Name

- **Required:** Yes
- **Length:** 1–100 characters
- **Uniqueness:** Per user (users can have multiple households with the same name, but a single user cannot create duplicates in a single operation)

### Household ID (UUID Format)

- Format: RFC 4122 UUID v4
- Example: `550e8400-e29b-41d4-a716-446655440000`

---

## Extension Points (Future)

When adding new onboarding tasks:

1. Add new task type to `OnboardingTaskType` enum (e.g., `'SetupProfile' | 'ConfigurePreferences'`)
2. Create corresponding endpoint(s) to complete the task
3. Update `OnboardingInfo.pendingTasks` to include the new task type
4. Extend `UserOnboarding.IsFullyOnboarded` logic on backend to check all required tasks
5. Update frontend to gate additional routes/features based on task completion

---

## Testing Strategy

### Backend Contract Tests

- Verify all endpoints return correct HTTP status codes
- Verify error responses include appropriate error messages
- Verify household list reflects user's available households
- Verify household creation marks onboarding complete

### Frontend Contract Tests

- Mock API responses for all endpoints
- Verify `OnboardingService` correctly parses responses
- Verify route guards enforce access control based on `isComplete` flag

### E2E Tests

- Full flow: login → list households → create/join → verify redirect
