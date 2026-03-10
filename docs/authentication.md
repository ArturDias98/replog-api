# Authentication

## Overview

Replog API uses a custom JWT-based authentication system. Users authenticate via Google OAuth on the client side, then exchange their Google ID token for API-issued access and refresh tokens.

## Auth Flow

1. Client authenticates with Google (OAuth 2.0) and receives a Google ID token
2. Client sends the Google ID token to `POST /api/auth/login`
3. API validates the Google ID token, creates or updates the user record, and returns an access token + refresh token
4. Client stores both tokens and uses the access token in the `Authorization` header for all subsequent API requests
5. When the access token expires (401 response), the client calls `POST /api/auth/refresh` to get a new pair of tokens

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
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "base64-encoded-random-string",
  "expiresAt": "2026-03-10T12:30:00Z"
}
```

**Error Responses:**

| Status | Reason |
|--------|--------|
| 400 | Missing or empty `googleIdToken` |
| 401 | Invalid or expired Google ID token |

### POST /api/auth/refresh

Exchanges an expired access token + valid refresh token for a new pair of tokens. No authentication required.

**Request:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "base64-encoded-random-string"
}
```

**Response (200 OK):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "new-base64-encoded-random-string",
  "expiresAt": "2026-03-10T12:45:00Z"
}
```

**Error Responses:**

| Status | Reason |
|--------|--------|
| 400 | Missing or empty `accessToken` or `refreshToken` |
| 401 | Invalid access token, user not found, refresh token expired, or refresh token mismatch |

> **Note:** After a successful refresh, a new refresh token is issued and the previous one is invalidated. Always store the new refresh token from the response.

## Token Details

| Token | Lifetime | Format |
|-------|----------|--------|
| Access token | 15 minutes | JWT (HS256) |
| Refresh token | 30 days | Base64-encoded random bytes |

The access token JWT contains the following claims:

| Claim | Description |
|-------|-------------|
| `sub` | User ID (Google subject identifier) |
| `email` | User's email address |
| `name` | User's display name |
| `picture` | User's avatar URL (only present if available) |
| `jti` | Unique token identifier |
| `iss` | Issuer (`replog-api`) |
| `aud` | Audience (`replog-client`) |
| `exp` | Expiration timestamp |

The client can decode the JWT payload (base64) to read user profile info (`name`, `picture`, `email`) without a separate API call.

## Client Integration Guide

### Making Authenticated Requests

Include the access token in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6...
```

### Handling Token Expiration

1. Store both `accessToken` and `refreshToken` after login or refresh
2. On any API call that returns **401 Unauthorized**:
   - Call `POST /api/auth/refresh` with the expired access token and stored refresh token
   - If refresh succeeds: store the new tokens and retry the original request
   - If refresh fails (401): redirect the user to Google login to re-authenticate
3. Use the `expiresAt` field to proactively refresh before expiration (recommended: refresh when less than 1 minute remains)

### Multi-Device Sessions

The API supports concurrent sessions across multiple devices. Each login generates an independent refresh token. Refreshing a token on one device does not affect other devices' sessions. Expired tokens are automatically cleaned up on each login.

### Logout

To log out, simply discard both tokens on the client side. There is no server-side logout endpoint — the access token will expire naturally, and the refresh token will become unused (cleaned up on next login).

### Token Storage Recommendations

- **Web apps:** Store tokens in memory (preferred) or `httpOnly` cookies. Avoid `localStorage` for the refresh token.
- **Mobile apps:** Use secure storage (Keychain on iOS, EncryptedSharedPreferences on Android).
