# BATON — Session Handoff Instructions

These instructions tell AI agents how to read and write BATON documents for
cross-session continuity. They are designed to be referenced from any project's
CLAUDE.md or GEMINI.md.

A **BATON** is a session handoff document that carries narrative context between
sessions. It tells the next session what happened, what decisions were made,
and what to focus on next.

BATONs are stored as posts in the Issue Tracker with the tag `baton`. When a
new BATON is created, the system automatically archives (closes) all previous
BATONs for that project.

**Related files:**
- `shared-instructions/project-config.json` — All constants
- `shared-instructions/tracker-api-helpers.py` — Python helper for API calls
- Your agent's `api-calling.md` — Shell-specific patterns and fallbacks

---

## At Session Start

1. **Retrieve the latest BATON** from the Issue Tracker:

```bash
python docs/shared-instructions/tracker-api-helpers.py baton
```

Read the returned `text` field to understand:
   - What was accomplished in the previous session
   - Key decisions and their rationale
   - Remaining work and recommended focus
   - How to start the project (build/run commands)

2. **If the Issue Tracker is available:** Also retrieve open issues
   (see `issue-tracking.md`).

3. **If the Issue Tracker is unavailable:** Work in BATON-only mode — write the
   BATON to a temporary local file and sync it next session (see Issue Fallback below).

---

## Determining the Session Number

The session number is determined from the Issue Tracker session that was created
for this session (see `issue-tracking.md`). Extract the session number from the
session name, or look at the latest BATON's title to determine the next number.

---

## At Session End

Before ending the session, create a new BATON post in the Issue Tracker.

### Creating a BATON Post

1. Write the BATON markdown content to a temporary file.
2. Submit it using the Python helper's `post-file` command.

**Claude (bash):**
```bash
cat > /tmp/baton-content.md << 'BATONEOF'
# BATON: Session NNN — Descriptive Title

**Project:** [Project name]
**Baton Created:** [Today's date, YYYY-MM-DD]
**Session:** NNN — [Session focus description]
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

[Narrative overview...]

### Key Decisions

[Bullet list of decisions and rationale...]

---

## Remaining Work

[Numbered list referencing tracker issue numbers...]

---

## How to Start

[Build/run/test commands...]

---

*Recommended focus for next session: [brief guidance].*
BATONEOF

python docs/shared-instructions/tracker-api-helpers.py post-file --agent claude \
    --action New --title "BATON NNN — Descriptive Title" --tags "baton" \
    --text-file /tmp/baton-content.md
```

**Gemini (PowerShell):**
```powershell
@"
# BATON: Session NNN — Descriptive Title

**Project:** [Project name]
...full markdown content...
"@ | Set-Content -Path "$env:TEMP\baton-content.md" -Encoding UTF8

python docs/shared-instructions/tracker-api-helpers.py post-file --agent gemini --action New --title "BATON NNN — Descriptive Title" --tags "baton" --text-file "$env:TEMP\baton-content.md"
```

The auto-archive system will automatically close all previous BATONs for this
project when the new one is created.

### Writing Guidelines

- **The BATON carries the story.** Focus on narrative context that helps the next
  session understand *why* things were done, not just *what*.
- **Keep it concise.** A BATON should be scannable in under 2 minutes.
- **Reference issue numbers** and direct the reader to the tracker for details.
  Do not duplicate issue data in the BATON.

---

## Issue Fallback (BATON-Only Mode)

When the Issue Tracker is unavailable and the user opts for BATON-only mode:

1. Write the BATON content to a temporary local file: `docs/BATON-PENDING.md`
2. Include an additional **Issues** section for any issues that arose:

```markdown
## Issues (BATON-Only — Sync to Tracker)

> These issues were tracked in the BATON because the Issue Tracker was unavailable.
> Sync them to the tracker at the start of the next session when the API is available.

### New Issues
- **[Title]** (tags: tag1, tag2) — [Description]

### Status Changes
- **#[ref]** [Title] — Changed from [old status] to [new status]. [Reason.]

### Resolved
- **#[ref]** [Title] — [Resolution details.]
```

At the start of the next session, if the Issue Tracker becomes available:
1. Create the BATON post via the helper (`post-file` command)
2. Create new issues via `post` commands
3. Apply status changes via appropriate ActionType posts
4. Delete the temporary `docs/BATON-PENDING.md` file
5. Once synced, the tracker is authoritative.
