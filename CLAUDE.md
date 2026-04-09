# CLAUDE.md — DabblerPdf Verification & Validation

## Project Overview

**DabblerPdf** — A scoped port of OpenPDF (Java, LGPL-2.1 / MPL-2.0) to idiomatic C# targeting .NET 10. Provides text measurement and coordinate-based PDF generation for the Dabbler.Report reporting engine.

**Why this exists:** The `access-to-blazor` project is migrating WPF controls to Blazor. The reporting engine needs:
1. **Text measurement** — font metrics for layout composition (`ITextMeasurer`)
2. **PDF generation** — coordinate-based rendering of pre-positioned report elements

Rather than depending on two external libraries (SkiaSharp + PdfSharp), we port the relevant subset of OpenPDF to have a single, self-owned library that does both.

**Key constraint:** This is a SCOPED port. Do NOT port the entire OpenPDF library — only the subset needed for text measurement and coordinate-based PDF rendering.

**Consumer:** The `access-to-blazor` project will reference this library via git submodule + project reference.

**License:** LGPL-2.1-only AND MPL-2.0 (matching OpenPDF's dual license). All source files must include the appropriate license header.

## AI Router

This project includes an AI Router module (`ai-router/`) for routing reasoning tasks to cheaper models and verifying results cross-provider.

### When to use the router

- Code review → `route(code, task_type="code-review")` ← auto-verified
- Security review → `route(code, task_type="security-review")` ← auto-verified
- Documentation → `route(code, task_type="documentation")`
- Analysis → `route(question, task_type="analysis")`
- Test generation → `route(code, task_type="test-generation")`

Do NOT route file creation, shell commands, or anything needing the filesystem.

### How to use it

The module folder is `ai-router/` (hyphen). Import via `importlib` since Python cannot import hyphenated names:

```python
import importlib.util, sys

def load_ai_router():
    spec = importlib.util.spec_from_file_location(
        'ai_router', 'ai-router/__init__.py',
        submodule_search_locations=['ai-router'])
    mod = importlib.util.module_from_spec(spec)
    sys.modules['ai_router'] = mod
    spec.loader.exec_module(mod)
    return mod

ar = load_ai_router()
route = ar.route

result = route(
    content="<the text to analyze>",
    task_type="code-review",
    context="<optional background>"
)
print(result.content)
print(result.total_cost_usd)
```

## Session Set Workflow

When the human says **"Start the next session"**, follow this procedure exactly:

### Step 0: Verify API Keys

Before doing anything else, load API keys from the Windows OS user environment and confirm both are present.
The bash shell does not inherit Windows user-scope environment variables, so they must be loaded explicitly:

```bash
export GEMINI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('GEMINI_API_KEY', 'User')" | tr -d '\r')
export ANTHROPIC_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('ANTHROPIC_API_KEY', 'User')" | tr -d '\r')
.venv/Scripts/python.exe utils/load_env.py
```

`utils/load_env.py` automates this for Python subprocesses. For the bash-level export above,
run the two `export` lines first, then verify with:

```bash
.venv/Scripts/python.exe -c "
import os
missing = [k for k in ('ANTHROPIC_API_KEY', 'GEMINI_API_KEY') if not os.environ.get(k)]
if missing:
    raise SystemExit(f'Missing environment variables: {missing}')
print('API keys OK')
"
```

If keys are still missing after the above steps, stop and tell the human — the keys may not be set at
the Windows user scope (`System.Environment.SetEnvironmentVariable(key, value, 'User')` sets them there).

Do not proceed past this step until both keys are confirmed present.

### Step 1: Identify the Active Session Set

Auto-detect the active session set using `find_active_session_set()`. Only fall back to the
**Active Session Set Override** section if detection is ambiguous.

```python
from ai_router.session_log import find_active_session_set

SESSION_SET = find_active_session_set("docs/session-sets")
print(f"Active session set: {SESSION_SET}")
```

Detection rules (in order):
1. A folder with `spec.md` + `activity-log.json` but **no** `change-log.md` → **in-progress** (use this)
2. A folder with `spec.md` only (no `activity-log.json`, no `change-log.md`) → **not-started** (use this if no in-progress exists)
3. A folder with `change-log.md` → **complete** (skip)

If the function raises an error (multiple candidates), check the **Active Session Set Override**
section below, set the folder path there, and use that value for `SESSION_SET` instead.

### Step 2: Read the Activity Log

Open `activity-log.json` in the session set folder.

- If the file doesn't exist, this is Session 1. Create it along with the `session-reviews/` and `issue-logs/` subdirectories.
- If it exists, determine the next session number:
  - Find the highest `sessionNumber` whose steps are all `"complete"` AND whose `session-reviews/session-NNN.md` file exists
  - Next session = that number + 1

```python
from ai_router.session_log import SessionLog

log = SessionLog(SESSION_SET)
next_session = log.get_next_session_number()
```

**After loading the log, verify the structure is consistent:**

- Confirm the highest completed session (as determined by `session-reviews/session-NNN.md` existing) has a build step AND a verification step logged. If either is missing, those steps were completed but not logged — note the gap in your response, then proceed with the correct `next_session` number.
- The canonical log format stores all steps via `log.log_step()`. If the file contains a mix of `sessions`, `session3`, or raw `entries` arrays from earlier inconsistent logging, treat `session-reviews/session-NNN.md` existence as the authoritative completion signal and continue — do not attempt to restructure the log file.

### Step 3: Read the Spec

Open `spec.md` in the session set folder. Find the session plan for the current session number (e.g., "Session 3 of 7: ...").

If this is Session 1, read the total number of sessions from the spec and set `log.total_sessions`.

### Step 4: Verify Prerequisites

Confirm that files listed in prior sessions' "Creates" and "Touches" sections actually exist. If something is missing, report it and stop.

### Step 5: Execute the Session

For each step in the session plan:

1. **If it requires creating or modifying files:** Do it directly.
2. **If it's a reasoning task** (review, analysis, documentation): Use `ai_router.route()` to route it to the cheapest capable model.
3. **Log the step** in `activity-log.json` using `log.log_step()`:
   ```python
   log.log_step(
       session_number=N,
       step_number=step_num,
       step_key="session-N/step-name",
       description="What was done",
       status="complete",
       api_calls=[{
           "model": result.model_name,
           "taskType": "code-review",
           "inputTokens": result.input_tokens,
           "outputTokens": result.output_tokens,
           "costUsd": result.cost_usd
       }]  # or [] if no router calls
   )
   ```
   > **Always use `log.log_step()` — never write JSON directly.** This includes the build step
   > (Step 6) and the verification step (Step 7). Skipping these leaves the log incomplete and
   > makes it impossible to detect missing steps in future sessions.

### Step 6: Build and Test

Run the project's build and test commands. All must pass before proceeding to verification.

```bash
dotnet build DabblerPdf.slnx
dotnet test tests/DabblerPdf.Tests/DabblerPdf.Tests.csproj
```

After the build, log the result with `log.log_step()` before moving on:

```python
log.log_step(N, step_num, "session-N/build",
    "dotnet build succeeded with 0 warnings, 0 errors. dotnet test: Passed X, Failed Y.",
    "complete", [])
```

### Step 7: End-of-Session Verification (MANDATORY)

This step is **not optional**. Every session must be independently verified.

> **CRITICAL: You (Claude) are the system under review. You MUST NOT perform this verification yourself.**
> The call to `route()` below must be executed as Python code via the ai_router module.
> `route()` will dispatch to a Gemini model — a completely separate AI provider — to check your work.
> If you cannot execute Python code, stop immediately and tell the human.

1. Collect all files created or modified during this session.
2. Extract the current session's spec excerpt (what was supposed to be built).
3. Execute the following Python code to route to a cross-provider (Gemini) verifier:
   ```python
   result = route(
       content=verification_prompt,  # spec excerpt + file contents + build results
       task_type="session-verification",
       complexity_hint=70
   )
   ```
4. **Save the verifier's raw output** to `session-reviews/session-NNN.md`:
   ```python
   log.save_session_review(
       session_number=N,
       review_text=result.content,
       round_number=1
   )
   ```
5. Do NOT edit the session review file. It is the verifier's untouched output.
6. **Log the verification step** with `log.log_step()`:
   ```python
   log.log_step(N, step_num, "session-N/verification",
       "Cross-provider verification with Gemini (1 round). Verdict: VERIFIED. Session review saved to session-reviews/session-NNN.md.",
       "complete", [{
           "model": result.model_name,
           "taskType": "session-verification",
           "inputTokens": result.input_tokens,
           "outputTokens": result.output_tokens,
           "costUsd": result.cost_usd
       }])
   ```

### Step 8: Handle Verification Result

**If VERIFIED:** Session is complete. No issue log is created. Proceed to Step 9.

**If ISSUES FOUND:**

1. Parse the issues from the verifier's response.
2. Create `issue-logs/session-NNN.json` with all issues:
   ```python
   issues = [
       {
           "issue": "description from verifier",
           "severity": "Major",
           "status": "open",
           "response": ""
       }
   ]
   ```
3. Attempt to fix each issue.
4. Update each issue's `status` to `"fixed"` or `"deferred"` and set `response` to explain what was done or why it was deferred.
5. Save the updated issue log:
   ```python
   log.save_issue_log(session_number=N, issues=issues)
   ```
6. Re-run verification (max 2 retries). Append each round to the session review:
   ```python
   log.save_session_review(
       session_number=N,
       review_text=result.content,
       round_number=2
   )
   ```
   **Escalate to Opus when issues are severe:** If the issue log contains any item with
   `"severity": "Major"` or `"severity": "Critical"`, use `complexity_hint=85` for the
   re-verification call. This routes to Opus (tier 3) instead of Gemini Pro, which is
   appropriate when a fix has architectural implications.
   ```python
   has_major = any(i["severity"] in ("Major", "Critical") for i in issues)
   result = route(
       content=verification_prompt,
       task_type="session-verification",
       complexity_hint=85 if has_major else 70
   )
   ```
7. If issues persist after retries, log remaining items as `"deferred"` — the human will decide.

### Step 9: Cost Report

```python
from ai_router import print_cost_report
print_cost_report(SESSION_SET)
```

### Step 9.5: Commit and Push

After a successful (VERIFIED) session, commit all changed files and push to the remote:

```bash
git add -A
git commit -m "Session N of M: <short description>

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
git push
```

Use the session number and a brief description matching the spec title.
Do **not** commit if verification returned ISSUES FOUND and issues are still open/deferred — wait for the human to decide.

### Step 9.75: Kill Orphaned conhost.exe Processes

After committing and pushing, clean up any conhost.exe processes left behind by .NET test/build runners:

```bash
.venv/Scripts/python.exe utils/kill_conhost.py
```

Run `--dry-run` first if you want to preview which processes will be killed before committing to it.

### Step 10: Last Session Only

If this is the last session (session number == total sessions from spec):

1. Generate `change-log.md` in the session set folder, documenting:
   - All new files created across all sessions
   - All existing files modified
   - New classes, methods, and parameters
   - Configuration changes
   - Test coverage summary
2. Use `route(task_type="documentation")` to help generate it from the actual source files.

### Important Rules

- **Do NOT execute more than one session.** If you finish early, tell the human. Let them decide whether to continue.
- **Do NOT skip verification.** It is mandatory for every session.
- **Do NOT edit session review files.** They are the verifier's raw output.
- **If prerequisites are missing,** stop and report. Don't guess.
- **Log every step** in `activity-log.json` via `log.log_step()` — including build and verification. It is the source of truth for progress. Never write JSON to the log file directly.

## Active Session Set Override

Used **only** when `find_active_session_set()` is ambiguous (multiple in-progress or multiple
unstarted session sets). Set the folder path here and use it for `SESSION_SET` in Step 1.
Leave blank when auto-detection works (the common case).

- **Folder:** *(auto-detect — set this only if Step 1 raises an ambiguity error)*

## Code Style

- Namespace: `DabblerPdf` (root), sub-namespaces: `DabblerPdf.Document`, `DabblerPdf.Pdf`, `DabblerPdf.Text`, `DabblerPdf.Image`, `DabblerPdf.Utilities`
- Nullable reference types enabled
- PascalCase public, `_camelCase` private
- All event handlers return `Task`, suffix `Async`
- License headers required on all source files

## Building & Testing

```bash
# Build
dotnet build DabblerPdf.slnx

# Run tests
dotnet test tests/DabblerPdf.Tests/DabblerPdf.Tests.csproj
```
