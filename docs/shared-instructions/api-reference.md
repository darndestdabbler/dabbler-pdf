# Issue Tracker API Reference

This file defines the Issue Tracker REST API endpoints, payload structures, and
semantics. It is **shell-agnostic** — it describes *what* to send, not *how* to
send it. For platform-specific calling patterns, see:

- `claude-instructions/api-calling.md` (Claude Code — bash / Python)
- `gemini-instructions/api-calling.md` (Gemini Code Assist — PowerShell / Python)

Both agents can also use the shared helper script `shared-instructions/tracker-api-helpers.py`.

---

## Configuration Constants

These must be defined in the project's agent-specific instruction file
(CLAUDE.md or GEMINI.md):

| Constant                    | Example value                  |
|-----------------------------|--------------------------------|
| `ISSUE_TRACKER_API_URL`     | `http://localhost:5124/api`    |
| `PROJECT_ID`                | `8`                            |

---

## Known Actor IDs

| Actor  | Id | Role   | Use when...                        |
|--------|----|---------|------------------------------------|
| Claude | 1  | AI     | Claude is the author (fromActorId) |
| Human  | 2  | Admin  | The user is the author or reviewer |
| System | 3  | System | System-generated entries           |
| Gemini | 4  | AI     | Gemini is the author               |

---

## Issue Ownership and Archive Enforcement

- **Owner** = the `fromActorId` on the root post (the actor who created the issue).
- **Only the owner, a delegated reviewer, or an Admin** can Archive (close) an issue.
- AI actors (Claude, Gemini) should use **Resolve** instead of Archive when they've
  addressed an issue they don't own.
- The API returns **403 Forbidden** if a non-owner/non-Admin/non-delegate attempts Archive.

**Delegation:** When any child post includes a `toActorId`, the root post's
`toActorId` is updated to track the current assignee. That assignee gains
Archive authority.

---

## IV&V Issue Lifecycle

When Gemini (the IV&V auditor) creates a finding:

1. **Gemini creates the issue** (`fromActorId: 4`, `actionType: New`) — Gemini is the owner.
2. **Claude resolves the issue** (`fromActorId: 1`, `actionType: Resolve`, `toActorId: 4`) —
   status → Pending Review. Route back to Gemini (not Human) for verification.
3. **Gemini verifies:**
   - Fix adequate → Gemini **archives** (Gemini is owner, so this is permitted). Status → Closed.
   - Fix insufficient → Gemini posts a **Discuss** reply explaining what's still wrong.
     Status remains Pending Review. Claude checks for Discuss replies at session start.

**How to detect IV&V issues:** If the root post's `fromActorId` is `4` (Gemini),
use `toActorId: 4` when resolving.

---

## Endpoints

### Projects

#### Register a project

```
POST /api/projects
Content-Type: application/json

{
  "name": "ProjectName"
}
```

Returns: `{ "id": 3, "name": "ProjectName" }`

---

### Sessions

#### Create a session

```
POST /api/sessions
Content-Type: application/json

{
  "projectId": PROJECT_ID,
  "name": "Session NNN — Description"
}
```

Returns: `{ "id": 5, "name": "...", "projectId": 3, "startDate": "..." }`

---

### Posts

#### Create a new issue (root post)

```
POST /api/posts
Content-Type: application/json

{
  "sessionId":    SESSION_ID,
  "fromActorId":  ACTOR_ID,
  "actionType":   "New",
  "title":        "Issue title",
  "tags":         "tag1,tag2",
  "text":         "Description of the issue. Markdown is allowed."
}
```

#### Add discussion to an existing post

```
POST /api/posts
Content-Type: application/json

{
  "sessionId":    SESSION_ID,
  "fromActorId":  ACTOR_ID,
  "actionType":   "Discuss",
  "actionForId":  POST_ID,
  "text":         "Discussion or investigation notes."
}
```

#### Resolve an issue (→ Pending Review)

```
POST /api/posts
Content-Type: application/json

{
  "sessionId":    SESSION_ID,
  "fromActorId":  ACTOR_ID,
  "actionType":   "Resolve",
  "actionForId":  POST_ID,
  "toActorId":    REVIEWER_ACTOR_ID,
  "text":         "Work complete. Details here."
}
```

#### Archive (close) an issue

**Restricted:** Owner, delegated reviewer, or Admin only.

```
POST /api/posts
Content-Type: application/json

{
  "sessionId":    SESSION_ID,
  "fromActorId":  ACTOR_ID,
  "actionType":   "Archive",
  "actionForId":  POST_ID,
  "text":         "Verified and closing."
}
```

#### Hold (defer) an issue

```
POST /api/posts
Content-Type: application/json

{
  "sessionId":    SESSION_ID,
  "fromActorId":  ACTOR_ID,
  "actionType":   "Hold",
  "actionForId":  POST_ID,
  "text":         "Reason for deferral."
}
```

#### Reopen an issue

```
POST /api/posts
Content-Type: application/json

{
  "sessionId":    SESSION_ID,
  "fromActorId":  ACTOR_ID,
  "actionType":   "Reopen",
  "actionForId":  POST_ID,
  "text":         "Reason for reopening."
}
```

#### Request user review

```
POST /api/posts
Content-Type: application/json

{
  "sessionId":    SESSION_ID,
  "fromActorId":  ACTOR_ID,
  "actionType":   "Check",
  "actionForId":  POST_ID,
  "toActorId":    2,
  "text":         "Please review this."
}
```

---

### Queries

| Purpose                        | Endpoint                                                  |
|--------------------------------|-----------------------------------------------------------|
| Open issues for project        | `GET /api/posts?status=Open&projectId=PROJECT_ID`         |
| Pending Review for project     | `GET /api/posts?status=Pending%20Review&projectId=PROJECT_ID` |
| All posts for a session        | `GET /api/posts?sessionId=SESSION_ID`                     |
| Full thread for a post         | `GET /api/posts/POST_ID/thread`                           |
| Filter by tags                 | `GET /api/posts?tags=tagname&status=Open&projectId=PROJECT_ID` |
| Latest BATON for project       | `GET /api/posts/latest-baton?projectId=PROJECT_ID`        |

---

## ActionType Reference

| ActionType        | Use when...                                          | Status effect on root | Access control         |
|-------------------|------------------------------------------------------|-----------------------|------------------------|
| New               | Opening a new issue or topic                         | → Open                | Anyone                 |
| Discuss           | Adding commentary or investigation notes             | (no change)           | Anyone                 |
| Proceed As Is     | Approving something without modification             | (no change)           | Anyone                 |
| Proceed With Mods | Approving with changes (describe in Text)            | (no change)           | Anyone                 |
| Check             | Requesting the user to review (set toActorId: 2)    | (no change)           | Anyone                 |
| Resolve           | Work complete, requesting owner/Admin review         | → Pending Review      | Anyone                 |
| Hold              | Pausing/deferring work                               | → Deferred            | Anyone                 |
| Archive           | Closing/resolving an issue (verified by owner)       | → Closed              | Owner, delegate, Admin |
| Reopen            | Reopening a closed, deferred, or pending issue       | → Open                | Anyone                 |

---

## Tag Taxonomy

| Category     | Tags                                                                                       |
|--------------|--------------------------------------------------------------------------------------------|
| Layer        | `layer:api`, `layer:presentation`, `layer:data`, `layer:tests`, `layer:cross-cutting`      |
| Control      | `control:grid`, `control:combobox`, `control:maskedtextbox`, `control:grpc`                |
| Finding type | `finding:security`, `finding:architecture`, `finding:quality`, `finding:performance`, `finding:accessibility`, `finding:fidelity` |
| Severity     | `sev:critical`, `sev:major`, `sev:minor`, `sev:info`                                      |
| Routing      | `needs:gemini`, `needs:human`, `needs:claude`, `disputed`                                  |
| Special      | `baton`                                                                                    |

---

## Finding Format (IV&V)

Every finding post MUST include:

1. **Title** prefixed with severity: `[SEV-CRITICAL]`, `[SEV-MAJOR]`, `[SEV-MINOR]`, or `[SEV-INFO]`
2. **File and line range** in the post text
3. **What was found** — specific, evidence-based
4. **Reference standard** — which guideline or rule this violates
5. **Recommendation** — a concrete fix, not just a problem statement

For red-team findings, also include:
6. **Attack vector** — how this could be exploited
7. **Impact** — what an attacker could achieve

---

## Severity Definitions

| Severity     | Definition                                                     |
|--------------|----------------------------------------------------------------|
| **Critical** | Security vulnerability, data loss, or architectural violation  |
| **Major**    | Significant quality or maintainability issue                   |
| **Minor**    | Code quality issue, not urgent                                 |
| **Info**     | Observation or suggestion                                      |
