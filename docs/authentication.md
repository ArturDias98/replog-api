# Authentication

## Overview

Replog API uses a custom JWT-based authentication system. Users authenticate via Google OAuth on the client side, then exchange their Google ID token for API-issued access and refresh tokens. Tokens are delivered as HttpOnly cookies — they are never exposed in the response body and cannot be accessed by JavaScript.

## Auth Flow

1. Client authenticates with Google (OAuth 2.0) and receives a Google ID token
2. Client sends the Google ID token to `POST /api/auth/login`
3. API validates the Google ID token, creates or updates the user record, and sets `access_token` and `refresh_token` as HttpOnly cookies
4. Browser automatically includes the cookies in all subsequent API requests — no manual token management required
5. When the access token expires (401 response), the client calls `POST /api/auth/refresh`; the API reads the cookies, rotates the tokens, and sets new cookies
6. To log out, the client calls `POST /api/auth/logout`, which clears the cookies

## Endpoints

### POST /api/auth/login

Exchanges a Google ID token for API tokens. No authentication required.

**Request:**

```json
{
  "googleIdToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6..."
}
```

**Response (200 OK):**

```json
{
  "expiresAt": "2026-03-10T12:30:00Z",
  "userId": "google-sub-123",
  "email": "user@example.com",
  "displayName": "Test User",
  "avatarUrl": "https://..."
}
```

Two HttpOnly cookies are set on the response:

| Cookie | Lifetime | Flags |
|--------|----------|-------|
| `access_token` | 15 minutes | `HttpOnly`, `SameSite=None`, `Secure` |
| `refresh_token` | 30 days | `HttpOnly`, `SameSite=None`, `Secure` |

**Error Responses:**

| Status | Error code             | Reason                                            |
|--------|------------------------|---------------------------------------------------|
| 400    | *(model binding)*      | Missing or empty `googleIdToken` (required field) |
| 401    | `invalid_google_token` | Google ID token is invalid or expired             |

---

### POST /api/auth/refresh

Rotates tokens using the cookies set by a previous login or refresh. No request body required. No authentication required.

**Request:** no body

**Response (200 OK):**

```json
{
  "expiresAt": "2026-03-10T12:45:00Z",
  "userId": "google-sub-123",
  "email": "user@example.com",
  "displayName": "Test User",
  "avatarUrl": "https://..."
}
```

New `access_token` and `refresh_token` cookies are set. The previous cookies are invalidated.

**Error Responses:**

| Status | Error code              | Reason                                               |
|--------|-------------------------|------------------------------------------------------|
| 401    | `missing_tokens`        | `access_token` or `refresh_token` cookie is absent   |
| 401    | `invalid_access_token`  | Access token cannot be parsed or has invalid signing |
| 401    | `user_not_found`        | User extracted from token no longer exists           |
| 401    | `invalid_refresh_token` | Refresh token does not match any stored token        |
| 401    | `token_expired`         | Refresh token has expired                            |

> **Note:** After a successful refresh, a new refresh token is issued and the previous one is invalidated. Tokens are rotated on every refresh call.

---

### POST /api/auth/logout

Clears the auth cookies. No authentication required.

**Request:** no body

**Response (200 OK):** empty body. The `access_token` and `refresh_token` cookies are deleted.

---

## Token Details

| Token | Lifetime | Format |
|-------|----------|--------|
| Access token | 15 minutes (configurable via `Jwt:AccessTokenExpirationMinutes`) | JWT (HS256) |
| Refresh token | 30 days (configurable via `Jwt:RefreshTokenExpirationDays`) | Base64-encoded random bytes |

The access token JWT contains the following claims:

| Claim | Description                          |
|-------|--------------------------------------|
| `sub` | User ID (Google subject identifier)  |
| `iss` | Issuer (`replog-api`)                |
| `aud` | Audience (`replog-client`)           |
| `exp` | Expiration timestamp                 |

---

## Client Integration Guide

### Making Authenticated Requests

No special headers are needed. The browser automatically includes the `access_token` cookie on every request to the API origin. The only requirement is that fetch/XHR calls include credentials:

```js
fetch('/api/sync/pull', { credentials: 'include' })
```

All three client origins (`http://localhost:4200` dev, `https://replog.adrvcode.com` prod, `https://localhost` Capacitor Android) are in the CORS allowlist. `credentials: 'include'` is required on every fetch/XHR call for cookies to be sent and received.

### Handling Token Expiration

1. Use the `expiresAt` field from the login/refresh response to schedule a proactive refresh (recommended: refresh when less than 1 minute remains)
2. On any API call that returns **401 Unauthorized**:
   - Call `POST /api/auth/refresh` (no body)
   - If refresh succeeds: store the new `expiresAt` and retry the original request
   - If refresh fails (401): redirect the user to Google login to re-authenticate

### Multi-Device Sessions

The API supports concurrent sessions across multiple devices. Each login generates an independent refresh token. Refreshing a token on one device does not affect other devices' sessions. Expired tokens are automatically cleaned up on each login.

### Logout

Call `POST /api/auth/logout` to clear the cookies server-side. The access token will expire naturally if the logout endpoint is not called (e.g. the browser is closed), but the refresh token will remain until it expires or is cleaned up on next login.

```js
await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
```

### Token Storage

Tokens are stored exclusively in HttpOnly cookies — they are never accessible via JavaScript. No client-side token storage is needed.
