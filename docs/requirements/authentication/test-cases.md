# Authentication - Test Cases

## Test Strategy

Testing will focus on verifying the security of the authentication flow, the correct enforcement of Role-Based Access Control (RBAC), and the integrity of the session management across the split frontend/backend architecture.

## TC-AUTH-001: BFF Login Flow (Web)

| Step | Action                                                     | Expected Result                                                                                                       |
| :--- | :--------------------------------------------------------- | :-------------------------------------------------------------------------------------------------------------------- |
| 1    | Navigate to the application root URL (unauthenticated).    | User is redirected to the backend login endpoint, then to the Microsoft Entra ID login page.                          |
| 2    | Enter valid credentials for a user with `Menlo.User` role. | User is authenticated and redirected back to the application.                                                         |
| 3    | Inspect browser developer tools (Application > Cookies).   | A session cookie is present. It is marked `HttpOnly`, `Secure`, and **`SameSite=Strict`**. Domain is `.wilcob.co.za`. |
| 4    | Inspect browser local storage / session storage.           | **No** access tokens or refresh tokens are visible in client-side storage.                                            |

## TC-AUTH-002: Role-Based Access Control (RBAC)

| Step | Action                                                                    | Expected Result                   |
| :--- | :------------------------------------------------------------------------ | :-------------------------------- |
| 1    | Log in as a user with `Menlo.Reader` role only.                           | Login is successful.              |
| 2    | Attempt to access a read-only resource (e.g., `GET /api/budget`).         | Request succeeds (200 OK).        |
| 3    | Attempt to access a write resource protected by `"CanEditBudget"` policy. | Request fails with 403 Forbidden. |
| 4    | Log in as a user with `Menlo.Admin` role.                                 | Login is successful.              |
| 5    | Attempt to access the same write resource.                                | Request succeeds (200/201).       |

## TC-AUTH-003: Token Auth (Mobile/API)

| Step | Action                                                                           | Expected Result                      |
| :--- | :------------------------------------------------------------------------------- | :----------------------------------- |
| 1    | Obtain a valid Bearer token for the App Registration via Postman/CLI.            | Token is received.                   |
| 2    | Make a request to `GET /api/budget` with `Authorization: Bearer <token>` header. | Request succeeds (200 OK).           |
| 3    | Make a request without the header.                                               | Request fails with 401 Unauthorized. |

## TC-AUTH-004: Session Expiration & Refresh

| Step | Action                                                                            | Expected Result                                                                                                       |
| :--- | :-------------------------------------------------------------------------------- | :-------------------------------------------------------------------------------------------------------------------- |
| 1    | Log in and wait for the short-lived Access Token to expire (simulated or actual). | Session remains active.                                                                                               |
| 2    | Perform an action in the UI.                                                      | Backend transparently uses the Refresh Token to get a new Access Token. The user action succeeds without redirection. |

## TC-AUTH-005: Logout

| Step | Action                       | Expected Result                                                                         |
| :--- | :--------------------------- | :-------------------------------------------------------------------------------------- |
| 1    | Click "Logout" in the UI.    | User is redirected to Entra ID logout page, then back to the app's public landing page. |
| 2    | Click "Back" in the browser. | Protected pages cannot be accessed.                                                     |
| 3    | Inspect cookies.             | The session cookie is removed or invalidated.                                           |
