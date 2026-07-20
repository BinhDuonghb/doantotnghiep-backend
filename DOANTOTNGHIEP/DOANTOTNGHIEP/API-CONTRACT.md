# DOANTOTNGHIEP API Contract

**Base URL:** `http://localhost:{port}` (HTTPS redirect on)
**Auth:** JWT Bearer token via `Authorization: Bearer <token>` header
**All dates:** ISO 8601 format (`yyyy-MM-ddTHH:mm:ssZ`)
**All IDs:** MongoDB ObjectId (24-character hex string)

---

## 1. Authentication

### `POST /api/auth/register` — Register new user (public)

**Request:**
```json
{
  "email": "string (required, valid email)",
  "password": "string (required, min 6 chars)",
  "confirmPassword": "string (required, must match password)",
  "name": "string (required)"         // JSON property: "name" → maps to FullName
}
```

**Response `201`:**
```json
{
  "user": {
    "id": "string (ObjectId)",
    "email": "string",
    "name": "string",
    "createdAt": "DateTime"
  },
  "accessToken": "string (JWT)",
  "refreshToken": "string",
  "expiresIn": 1800
}
```

**Errors:** `400` validation / `409` email already exists

---

### `POST /api/auth/login` — Login (public)

**Request:**
```json
{
  "email": "string (required)",
  "password": "string (required)"
}
```

**Response `200`:** Same shape as register (`AuthResponse`)

**Errors:** `400` validation / `401` invalid credentials

---

### `POST /api/auth/refresh` — Refresh access token (public)

**Request:**
```json
{
  "refreshToken": "string (required)"
}
```

**Response `200`:**
```json
{
  "accessToken": "string (new JWT)",
  "refreshToken": "string (new refresh token — old one is revoked)",
  "expiresIn": 1800
}
```

**Errors:** `400` validation / `401` invalid/expired/revoked token

---

### `GET /api/auth/me` — Get current user (authenticated)

**Response `200`:**
```json
{
  "id": "string",
  "email": "string",
  "name": "string",
  "createdAt": "DateTime"
}
```

**Errors:** `401` unauthorized

---

### `POST /api/auth/logout` — Logout (public)

**Request:**
```json
{
  "refreshToken": "string (required)"
}
```

**Response `200`:**
```json
{
  "message": "Successfully logged out"
}
```

**Errors:** `400` invalid/expired token

---

## 2. Targets

All target endpoints require `[Authorize]`. Ownership is verified on every operation.

### `GET /api/targets` — List my targets

**Response `200`:**
```json
{
  "targets": [
    {
      "id": "string (ObjectId)",
      "userId": "string",
      "title": "string",
      "description": "string?",
      "duration": 0,
      "durationUnit": "hour|day|week|month",
      "status": "not_started|in_progress|completed",
      "startedAt": "string? (ISO 8601)",
      "completedAt": "string? (ISO 8601)",
      "hasPlan": false,
      "createdAt": "string (ISO 8601)",
      "updatedAt": "string (ISO 8601)"
    }
  ],
  "totalCount": 1
}
```

**Errors:** `401` unauthorized / `404` user not found / `500` server error

---

### `POST /api/targets` — Create target

**Request:**
```json
{
  "title": "string (required, 1-100 chars)",
  "description": "string? (max 500 chars)",
  "duration": 3,
  "durationUnit": "day|hour|week|month (default: day)"
}
```

**Response `201`:** Single `TargetResponseDto` (same shape as list item above)
**Errors:** `400` validation / `401` unauthorized / `500` server error

---

### `GET /api/targets/{targetId}` — Target detail (includes plan + stats)

**Response `200`:**
```json
{
  "id": "string",
  "userId": "string",
  "title": "string",
  "description": "string?",
  "duration": 0,
  "durationUnit": "string",
  "status": "string",
  "startedAt": "string?",
  "completedAt": "string?",
  "hasPlan": true,
  "createdAt": "string",
  "updatedAt": "string",
  "plan": {
    "summary": "string (overall plan summary from AI)",
    "generatedAt": "string (ISO 8601)",
    "milestones": [
      {
        "title": "string",
        "description": "string",
        "order": 1,
        "durationEstimate": "string (e.g. '1 week')",
        "checklistItems": [
          {
            "title": "string (task description)",
            "isCompleted": false
          }
        ]
      }
    ],
    "quizzes": [
      {
        "question": "string",
        "options": ["string", "string", "string", "string"],
        "correctAnswer": "string (must match one option)",
        "milestoneTitle": "string (which milestone this quiz belongs to)"
      }
    ]
  },
  "totalMilestones": 4,
  "completedChecklistItems": 3,
  "totalChecklistItems": 12
}
```

Note: `plan` is `null` if no plan has been generated yet.
Note: `hasPlan` is `true` when a plan exists, `false` otherwise.

**Errors:** `401` unauthorized / `404` target not found / `500` server error

---

### `PUT /api/targets/{targetId}` — Update target

**Request:**
```json
{
  "title": "string? (1-100 chars)",
  "description": "string? (max 500 chars)",
  "duration": 3,
  "durationUnit": "hour|day|week|month",
  "status": "not_started|in_progress|completed"
}
```
All fields optional — only provided fields are updated.

**Status transitions:**
- Setting status to `in_progress` auto-sets `startedAt` if not already set
- Setting status to `completed` auto-sets `completedAt` (and backfills `startedAt` if needed)

**Response `200`:** `TargetResponseDto` (same as list item shape)
**Errors:** `400` validation / `401` unauthorized / `404` not found / `500` server error

---

### `DELETE /api/targets/{targetId}` — Delete target

**Response `200`:**
```json
{
  "success": true,
  "message": "Target deleted successfully"
}
```

**Errors:** `401` unauthorized / `404` not found / `500` server error

---

### `GET /api/targets/{targetId}/plan` — Get plan only

**Response `200`:** `PlanDataDto` object (same shape as the `plan` field in target detail)
```json
{
  "summary": "string",
  "generatedAt": "string (ISO 8601)",
  "milestones": [ ... ],
  "quizzes": [ ... ]
}
```

**Errors:** `401` unauthorized / `404` target not found OR plan not generated yet / `500` server error

---

### `PATCH /api/targets/{targetId}/milestones/{milestoneOrder}/checklist/{checklistIndex}` — Toggle checklist item

- `milestoneOrder`: 1-based order number of the milestone (e.g., `1` for first milestone)
- `checklistIndex`: 0-based index of the checklist item within that milestone's `checklistItems` array

**Request:**
```json
{
  "isCompleted": true
}
```
Set to `true` to mark done, `false` to mark undone.

**Response `200`:** `TargetDetailResponseDto` — full target detail with updated plan + stats
**Errors:** `400` index out of range OR target has no plan / `401` unauthorized / `404` target/milestone not found / `500` server error

---

## 3. Plan Generation

All plan endpoints require `[Authorize]`.

### `POST /api/plan/generate` — Generate or refine AI plan

**One endpoint, two modes** based on whether `feedback` is provided:

**Mode 1 — Initial generation:**
```json
{
  "targetId": "string (required)",
  "feedback": null
}
```
Calls AI to create a brand-new structured plan from scratch.

**Mode 2 — Regenerate with feedback:**
```json
{
  "targetId": "string (required)",
  "feedback": "Break milestone 2 into smaller steps. Add more detail to the checklist."
}
```
Sends the **existing plan** (including all `isCompleted` states) + your feedback to the AI. The AI modifies only what you asked. Completed items stay completed. Unchanged milestones stay identical.

**Response `201`:**
```json
{
  "targetId": "string",
  "plan": {
    "summary": "string",
    "generatedAt": "string (ISO 8601)",
    "milestones": [
      {
        "title": "string",
        "description": "string",
        "order": 1,
        "durationEstimate": "string",
        "checklistItems": [
          {
            "title": "string",
            "isCompleted": false
          }
        ]
      }
    ],
    "quizzes": [
      {
        "question": "string",
        "options": ["string", "string", "string", "string"],
        "correctAnswer": "string",
        "milestoneTitle": "string"
      }
    ]
  },
  "message": "Plan generated successfully." | "Plan refined successfully based on your feedback."
}
```

**Errors:** `400` validation / `401` unauthorized / `403` not your target / `404` target not found / `500` server error / `503` AI service unavailable

---

## 4. Quiz Generation

### `GET /api/quiz` — Get quizzes (requires user to have a target)

**Response `200`:**
```json
{
  "quizzes": [
    {
      "id": "string (ObjectId)",
      "question": "string",
      "options": ["string", "string", "string", "string"],
      "correctAnswer": "string",
      "category": "string",
      "difficulty": "Easy|Medium|Hard"
    }
  ],
  "userTargetSet": true
}
```

**Errors:** `401` unauthorized / `403` user has no target set / `500` server error

---

### `POST /api/quiz/generate-from-target` — AI quiz generation

**Request:**
```json
{
  "targetId": "string? (optional — uses first target if omitted)",
  "count": 5,
  "category": "string? (optional — 'default' if omitted)",
  "difficulty": "Easy|Medium|Hard (default: Medium)"
}
```
- `count`: 1–20, default 5
- `difficulty`: bound to prompt templates (Easy/Medium/Hard difficulty instructions)
- `category`: acts as the prompt template lookup key (matches against template names like "vocabulary", "grammar", etc., or falls back to "*")

**Response `201`:**
```json
{
  "generated": 5,
  "quizzes": [
    {
      "id": "string (ObjectId)",
      "question": "string",
      "options": ["string", "string", "string", "string"],
      "correctAnswer": "string",
      "category": "string",
      "difficulty": "string"
    }
  ],
  "targetUsed": {
    "id": "string",
    "title": "string",
    "description": "string?",
    "status": "string"
  }
}
```

**Errors:** `400` validation / `401` unauthorized / `403` no target / `404` target not found / `500` server error / `503` AI service unavailable

---

### `POST /api/admin/quiz/generate` — Admin static quiz generation (Admin only)

**Request:**
```json
{
  "count": 10,
  "category": "vocabulary|grammar|reading",
  "difficulty": "Easy|Medium|Hard"
}
```

**Response `201`:**
```json
{
  "generated": 10,
  "quizzes": [ ... ]
}
```

**Note:** This is template-based (not AI). Uses hardcoded quiz templates per category. Admin policy required.

---

### `GET /api/quiz/generation-status/{generationId}` — Placeholder (sync currently)

**Response `200`:**
```json
{
  "generationId": "string",
  "status": "completed",
  "generatedCount": 0,
  "requestedCount": 0,
  "error": null,
  "createdAt": "string (ISO 8601)",
  "completedAt": "string? (ISO 8601)"
}
```

**Note:** Current implementation is synchronous — this endpoint returns static `completed` status. Will be meaningful when async generation is added.

---

## 5. Health

### `GET /api/health` — Health check (public)

**Response `200` (Healthy/Degraded):**
```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "timestamp": "DateTime",
  "details": [
    {
      "name": "API|Database",
      "status": "Healthy|Degraded|Unhealthy",
      "description": "string?",
      "durationMs": 0
    }
  ]
}
```

**Response `503` (Unhealthy):** Same shape but HTTP 503

---

## 6. Shared Error Response Shape

All error responses use ASP.NET Core `ProblemDetails`:
```json
{
  "title": "string (short error title)",
  "status": 400,
  "detail": "string (human-readable explanation)"
}
```

Validation errors use `ValidationProblemDetails` which includes an `errors` dictionary:
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "title": ["Title must be at least 1 character long"],
    "durationUnit": ["DurationUnit must be one of: hour, day, week, month"]
  }
}
```

---

## 7. Typical User Flow

```
1. POST /api/auth/register     → create account, get tokens
2. POST /api/auth/login        → (future sessions) login, get tokens
   [Store accessToken, use in Authorization header for all subsequent calls]
   [Token expires in 30 min → POST /api/auth/refresh to get new pair]

3. POST /api/targets           → create a target
   {
     "title": "Build a React dashboard app",
     "description": "I want to learn React by building a full dashboard with charts, tables, and auth",
     "duration": 2,
     "durationUnit": "month"
   }

4. POST /api/plan/generate     → AI creates study plan
   { "targetId": "<id from step 3>" }
   → Returns plan with milestones like:
     Milestone 1: "React Fundamentals" (1 week)
       ☐ Set up dev environment
       ☐ Learn JSX and components
       ☐ Understand state and props
     Milestone 2: "Building the Dashboard UI" (2 weeks)
       ☐ Create layout component
       ☐ Implement navigation
       ...

5. Work through the plan:
   GET /api/targets/{id}       → see current state + progress stats
   GET /api/targets/{id}/plan  → see just the plan

6. Mark tasks done:
   PATCH /api/targets/{id}/milestones/1/checklist/0
   { "isCompleted": true }
   → "Set up dev environment" ✅

7. Mid-course correction:
   POST /api/plan/generate
   {
     "targetId": "<id>",
     "feedback": "I finished milestone 1 faster than expected. Make milestone 2 more challenging with 2 extra tasks."
   }
   → AI refines plan keeping all completed items, adding 2 new tasks to milestone 2

8. Generate quizzes to test knowledge:
   POST /api/quiz/generate-from-target
   { "targetId": "<id>", "count": 10, "category": "react", "difficulty": "Medium" }

9. Mark target complete:
   PUT /api/targets/{id}
   { "status": "completed" }
   → Auto-sets completedAt
```