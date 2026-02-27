# RepLog Backend — Sync API

Backend specification for the RepLog sync system. This API receives change events from the client, applies them to the database with conflict resolution, and returns the current state for the client to merge.

## Table of Contents

1. [Overview](#1-overview)
2. [Data Model (DynamoDB)](#2-data-model-dynamodb)
3. [API Endpoints](#3-api-endpoints)
4. [Conflict Resolution](#4-conflict-resolution)
5. [Authentication & Authorization](#5-authentication--authorization)
6. [Security](#6-security)

---

## 1. Overview

### What the backend does

- Receives batches of change events (create, update, delete) from the client
- Applies them to the database, resolving conflicts with last-write-wins
- Returns the current state as nested `WorkOutGroup[]` for the client to merge
- Manages sync metadata (`createdAt`, `updatedAt`, `deletedAt`) — the client does not track these
- Serves as the source of truth for cross-device consistency

### What the backend does NOT do

- Real-time sync (WebSockets, SSE) — sync is pull-based
- Multi-user collaboration — this is a single-user personal app
- Client state management — the client manages its own IndexedDB and sync queue

### Design Principles

- **Idempotent push** — replaying the same change is safe (CREATE skips duplicates, UPDATE uses timestamp comparison, DELETE checks `deletedAt`)
- **Last-write-wins** — conflicts are resolved by comparing timestamps, no user intervention needed
- **Cascading deletes** — deleting a workout deletes all its children
- **Document storage** — each workout is stored as a single DynamoDB item containing the full nested structure (muscle groups, exercises, logs), matching the client's data model

---

## 2. Data Model (DynamoDB)

### 2.1 Table: `replog-workouts`

Each item is a workout document, keyed by the workout's UUID.

#### Key Schema

| PK (`id`) | Description |
|---|---|
| `<workoutId>` | Workout UUID (partition key, no sort key) |

#### GSI: `UserIdIndex`

| GSI PK (`userId`) | Use |
|---|---|
| `<userId>` | Query all workouts for a user |

### 2.2 Item Schema

The workout item stores the entire nested structure as a single document.

| Attribute | Type | Description |
|---|---|---|
| `id` | S (PK) | Workout UUID |
| `userId` | S | Google Auth user ID (GSI PK) |
| `title` | S | Workout name |
| `date` | S | Workout date (YYYY-MM-DD) |
| `orderIndex` | N | Position in list |
| `muscleGroup` | L | List of muscle group objects (nested) |
| `createdAt` | S | ISO 8601 timestamp |
| `updatedAt` | S | ISO 8601 timestamp |
| `deletedAt` | S / null | ISO 8601 timestamp or absent |

The `muscleGroup` attribute contains the full hierarchy:

```json
{
  "id": "w-uuid-1",
  "userId": "google-123",
  "title": "Push Day",
  "date": "2026-02-25",
  "orderIndex": 0,
  "muscleGroup": [
    {
      "id": "mg-uuid-1",
      "workoutId": "w-uuid-1",
      "title": "Chest",
      "date": "2026-02-25",
      "orderIndex": 0,
      "exercises": [
        {
          "id": "ex-uuid-1",
          "muscleGroupId": "mg-uuid-1",
          "title": "Bench Press",
          "orderIndex": 0,
          "log": [
            {
              "id": "log-uuid-1",
              "numberReps": 10,
              "maxWeight": 80,
              "date": "2026-02-25T10:00:00.000Z"
            }
          ]
        }
      ]
    }
  ],
  "createdAt": "2026-02-25T10:00:00.000Z",
  "updatedAt": "2026-02-25T10:05:00.000Z"
}
```

### 2.3 Access Patterns

| Access Pattern | Method |
|---|---|
| Get all workouts for a user | GSI `UserIdIndex`: `userId = <userId>`, filter `deletedAt` absent |
| Get a specific workout | `GetItem(id = <workoutId>)` |
| Find workout by ID (for child changes) | `GetItem(id = <workoutId>)` |

### 2.4 DynamoDB Item Size

DynamoDB has a 400 KB item size limit. A workout with 10 muscle groups, each with 10 exercises, each with 50 logs, fits well within this limit. If a workout ever approaches the limit, it would mean thousands of logs — unlikely for a workout tracker.

---

## 3. API Endpoints

All endpoints require authentication. The `userId` is extracted from the auth token — never from the request body.

### 3.1 `POST /api/sync/push`

Receives a batch of changes from the client and applies them.

**Request:**

```json
{
  "changes": [
    {
      "id": "change-uuid",
      "entityType": "workout",
      "entityId": "entity-uuid",
      "action": "CREATE",
      "timestamp": "2026-02-25T10:00:00.000Z",
      "data": {
        "id": "entity-uuid",
        "title": "Push Day",
        "date": "2026-02-25",
        "userId": "user-123",
        "orderIndex": 0
      },
      "parentId": null
    }
  ],
  "lastSyncedAt": "2026-02-24T20:00:00.000Z"
}
```

**Response (200 OK):**

```json
{
  "acknowledgedChangeIds": ["change-uuid-1", "change-uuid-2"],
  "conflicts": [
    {
      "changeId": "change-uuid-3",
      "resolution": "server_wins",
      "serverVersion": { "...entity fields..." }
    }
  ],
  "serverTimestamp": "2026-02-25T10:05:00.000Z"
}
```

**Response (409 — full re-sync needed):**

```json
{
  "error": "full_sync_required",
  "message": "Server state has diverged too much. Perform a full sync."
}
```

**Processing logic:**

1. Validate auth token, extract `userId`.
2. Validate request body schema.
3. Process each change sequentially (ordered by `timestamp`).
4. Apply each change to the database. Idempotency is inherent (duplicate CREATEs are skipped, UPDATEs use timestamp comparison, DELETEs check `deletedAt`).
5. Return acknowledged IDs, conflicts, and current server timestamp.

### 3.2 `GET /api/sync/pull`

Returns all workouts for the authenticated user, as nested `WorkOutGroup[]`.

**Response (200 OK):**

```json
{
  "workouts": [
    {
      "id": "w-uuid",
      "title": "Push Day",
      "date": "2026-02-25",
      "userId": "user-123",
      "orderIndex": 0,
      "muscleGroup": [
        {
          "id": "mg-uuid",
          "workoutId": "w-uuid",
          "title": "Chest",
          "date": "2026-02-25",
          "orderIndex": 0,
          "exercises": [
            {
              "id": "ex-uuid",
              "muscleGroupId": "mg-uuid",
              "title": "Bench Press",
              "orderIndex": 0,
              "log": [
                {
                  "id": "log-uuid",
                  "numberReps": 10,
                  "maxWeight": 80,
                  "date": "2026-02-25T10:00:00.000Z"
                }
              ]
            }
          ]
        }
      ]
    }
  ],
  "serverTimestamp": "2026-02-25T12:00:00.000Z"
}
```

**Processing logic:**

1. Validate auth token, extract `userId`.
2. Query GSI `UserIdIndex`: `userId = <userId>`, filter `deletedAt` absent.
3. Strip internal attributes (`userId`, `createdAt`, `updatedAt`) from each item.
4. Return the items as `workouts[]` — they are already in the nested `WorkOutGroup` format.

---

## 4. Conflict Resolution

### 4.1 Strategy: Last-Write-Wins (per workout)

Since the entire workout is stored as a single document, conflict resolution happens at the workout level. The version with the later `updatedAt` timestamp wins.

### 4.2 Rules

| Scenario | Resolution |
|---|---|
| CREATE — entity doesn't exist | Insert |
| CREATE — entity already exists | Skip (duplicate) |
| UPDATE — parent workout not found | Skip (orphaned) |
| UPDATE — parent workout deleted | Skip |
| UPDATE — `workout.updatedAt > change.timestamp` | Server wins — return conflict |
| UPDATE — `workout.updatedAt <= change.timestamp` | Apply update |
| DELETE — entity doesn't exist | Skip |
| DELETE — parent workout deleted | Skip |
| DELETE — entity exists | Remove from document |

### 4.3 Conflict Response Format

```json
{
  "changeId": "change-uuid-3",
  "resolution": "server_wins",
  "serverVersion": {
    "id": "entity-uuid",
    "title": "Server Title",
    "date": "2026-02-25"
  }
}
```

---

## 5. Authentication & Authorization

### 5.1 Authentication

- All endpoints require a valid Google Auth JWT in the `Authorization` header: `Bearer <token>`.
- Validate the JWT against Google's public keys.
- Extract `userId` (Google `sub` claim) from the token.
- No user table or profile item needed — the `userId` from the JWT is stored as an attribute on each workout item.

### 5.2 Authorization

- **Push:** For CREATE, the backend sets `userId` from the auth token. For UPDATE/DELETE, the backend verifies `workout.userId` matches the authenticated user before applying changes.
- **Pull/Full:** GSI query uses `userId` from the auth token — only the user's own data is returned.

### 5.3 User ID Override

The backend **ignores** the `userId` field in client data and always derives it from the auth token.

---

## 6. Security

### 6.1 Transport

- All endpoints must be served over HTTPS.

### 6.2 Input Validation

- Validate all incoming fields against the expected schema (types, required fields, string lengths).
- Reject unknown `entityType` values.
- Reject changes with missing required fields.
- Sanitize text fields (titles) to prevent stored XSS.
- Maximum string lengths: `title` — 200 chars, `date` — 10 chars (YYYY-MM-DD).

### 6.3 Rate Limiting

- Max 10 sync requests per minute per user.
- Max 100 changes per push request.

### 6.4 Idempotency

Push is inherently idempotent — no separate tracking needed:

- **CREATE:** If the entity already exists, the change is skipped.
- **UPDATE:** Timestamp comparison ensures stale updates are rejected.
- **DELETE:** If already deleted (`deletedAt` set), the change is skipped.

### 6.5 Cleanup

- Periodically purge soft-deleted workout items older than a configurable threshold (e.g., 90 days).
