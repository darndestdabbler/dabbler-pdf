# Issue Tracking — Cross-Session Instructions

These instructions tell AI agents how to use the Issue Tracker for cross-session
issue tracking. They are designed to be referenced from any project's CLAUDE.md
or GEMINI.md.

**Related files:**
- `shared-instructions/project-config.json` — All constants (API URL, project ID,
  actor IDs, session IDs)
- `shared-instructions/api-reference.md` — Endpoint definitions, payload schemas,
  ActionType reference, tag taxonomy, severity definitions
- `shared-instructions/tracker-api-helpers.py` — Python helper for making API calls
- `claude-instructions/api-calling.md` — Claude-specific calling patterns
- `gemini-instructions/api-calling.md` — Gemini-specific calling patterns

---

## Prerequisites

The Issue Tracker app must be running before making API calls:

```bash
cd "{{ISSUE_TRACKER_PATH}}/IssueTracker.Web"
dotnet run
```

To use a project-specific SQLite database, set the `ISSUETRACKER_DB_PATH` environment variable:

```bash
ISSUETRACKER_DB_PATH=/path/to/project/issues.db dotnet run --project "{{ISSUE_TRACKER_PATH}}/IssueTracker.Web/"
```

---

## First-Session Detection

At the start of every session, check whether `projectId` in `project-config.json`
has a value.

**If `projectId` is null**, this is the first session:

1. Prompt the user: "This appears to be the first session for this project.
   Would you like to register it with the Issue Tracker?"
2. If the user approves, register the project:

```bash
python docs/shared-instructions/tracker-api-helpers.py init --name "ProjectName"
```

This calls `POST /api/projects`, receives the auto-generated ID, and writes both
`projectId` and `projectName` back to `project-config.json`.

---

## At Session Start

After first-session detection is resolved:

1. **Create a session.** This writes the session ID to `project-config.json`
   so subsequent commands use it automatically.

```bash
# Claude
python docs/shared-instructions/tracker-api-helpers.py session \
    --agent claude --name "Session NNN — Description"

# Gemini
python docs/shared-instructions/tracker-api-helpers.py session \
    --agent gemini --name "Gemini IV&V - Mission N"
```

2. **Retrieve open and pending-review issues:**

```bash
python docs/shared-instructions/tracker-api-helpers.py get posts --status Open
python docs/shared-instructions/tracker-api-helpers.py get posts --status "Pending Review"
```

---

## During Work — When to Log Posts

Log a post when something needs to survive beyond this session:

- A **decision** was made (actionType: `New` or `Discuss`)
- A **blocker or open question** exists (actionType: `New`)
- Work is **deferred** (actionType: `Hold`)
- An item has been **addressed and is ready for review** (actionType: `Resolve`)
- A **review** is requested from the user (actionType: `Check`, `--to human`)

**Important:** AI agents should use **Resolve** (not Archive) when work on an
issue is complete, unless they are the owner. Resolve sets the status to
"Pending Review" and signals the appropriate reviewer to verify.

### IV&V Issue Lifecycle

See `api-reference.md` § "IV&V Issue Lifecycle" for the full three-step handoff
between Gemini (auditor), Claude (implementer), and Gemini (verifier).

**Key rule for Claude:** When resolving a Gemini finding, use `--to gemini`
to route back to the auditor for verification.

**Key rule for Gemini:** When verifying your own findings, use `--action Archive`.
When verifying Claude's findings, use `--action Resolve --to human`.

### Logging Guidelines

- **Do not log** routine implementation steps — only things with cross-session significance.
- **Bundle related items** — create a single issue with a checklist rather than
  separate issues for each related finding.

---

## API Unavailable Fallback

If the API is unreachable:

1. Prompt the user: "The Issue Tracker API is not reachable. Would you like to
   work in BATON-only mode for this session?"
2. If agreed, note issues in the BATON document instead (see `baton.md` for the
   fallback format).
3. Sync BATON-tracked issues to the Issue Tracker next session when the API
   becomes available.
