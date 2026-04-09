# API Calling — Gemini Code Assist Instructions

This file tells Gemini Code Assist how to make Issue Tracker API calls
from the VS Code integrated terminal on Windows.
For endpoint definitions and payload schemas, see `shared-instructions/api-reference.md`.
All constants (API URL, project ID, actor IDs, session ID) are in
`shared-instructions/project-config.json` — you do not need to pass them manually.

---

## The Problem This Solves

JSON payloads containing markdown text (newlines, quotes, asterisks, backticks)
break when embedded directly in `curl.exe` command strings. The shell interprets
special characters before curl receives them. **Never manually escape markdown
inside a curl `-d` argument.** Use the Python helper or PowerShell hashtables instead.

---

## Preferred Method: Python Helper Script

Use the shared helper at `docs/shared-instructions/tracker-api-helpers.py`.
It reads `project-config.json` for all constants and handles JSON serialization.

Run via the **terminal** tool:

### Session Start

```powershell
# Create a session (writes currentGeminiSessionId to config)
python docs/shared-instructions/tracker-api-helpers.py session --agent gemini --name "Gemini IV&V - Mission N"

# Retrieve open issues (projectId read from config)
python docs/shared-instructions/tracker-api-helpers.py get posts --status Open

# Retrieve Pending Review issues
python docs/shared-instructions/tracker-api-helpers.py get posts --status "Pending Review"

# Read the latest BATON
python docs/shared-instructions/tracker-api-helpers.py baton
```

### Posting Findings

```powershell
# Short text — inline
python docs/shared-instructions/tracker-api-helpers.py post --agent gemini --action New --title "[SEV-MAJOR] Issue title" --tags "finding:quality,sev:major" --text "Description."

# Markdown body — write to a temp file first, then submit
python docs/shared-instructions/tracker-api-helpers.py post-file --agent gemini --action New --title "[SEV-MAJOR] Complex finding" --tags "finding:quality,sev:major" --text-file C:\temp\finding-body.md
```

To write the temp file in PowerShell (here-strings handle all special characters):

```powershell
@"
File: src/DabblerPdf/Text/BaseFont.cs, Lines 45-62

Description with **markdown**.

### Recommendation
Fix description.
"@ | Set-Content -Path "$env:TEMP\finding.md" -Encoding UTF8

python docs/shared-instructions/tracker-api-helpers.py post-file --agent gemini --action New --title "[SEV-MAJOR] Finding title" --tags "finding:quality,sev:major" --text-file "$env:TEMP\finding.md"
```

### Verification Verdicts

```powershell
# Fix adequate — archive your own finding
python docs/shared-instructions/tracker-api-helpers.py post --agent gemini --action Archive --for 790 --text "Verified. Fix is correct."

# Fix adequate — resolve Claude's finding (you can't archive it)
python docs/shared-instructions/tracker-api-helpers.py post --agent gemini --action Resolve --for 123 --to human --text "Verified. Fix is correct."

# Fix insufficient — discuss
python docs/shared-instructions/tracker-api-helpers.py post --agent gemini --action Discuss --for 790 --text "Fix insufficient. Reason."
```

### Queries

```powershell
python docs/shared-instructions/tracker-api-helpers.py get posts --status Open
python docs/shared-instructions/tracker-api-helpers.py get posts/790/thread
python docs/shared-instructions/tracker-api-helpers.py get posts --status Open --tags "sev:critical"
python docs/shared-instructions/tracker-api-helpers.py config
```

---

## Alternative: PowerShell Native (Invoke-RestMethod)

If the Python helper is unavailable, use PowerShell directly.
Build payloads as **hashtables** and let `ConvertTo-Json` handle serialization.

```powershell
$text = @"
Description with **markdown**.
"@

$payload = @{
    sessionId   = 5
    fromActorId = 4
    actionType  = "New"
    title       = "Issue title"
    tags        = "tag1,tag2"
    text        = $text
}

$json = $payload | ConvertTo-Json -Depth 10
$response = Invoke-RestMethod -Uri "http://localhost:5124/api/posts" `
    -Method Post -Body $json -ContentType "application/json"
Write-Output $response
```

---

## What NOT To Do

These patterns cause escaping failures. **Do not use them:**

```powershell
# BAD: Markdown in a curl.exe -d string
curl.exe -s -X POST http://localhost:5124/api/posts -H "Content-Type: application/json" -d "{\"text\":\"**bold** and `code` breaks\"}"
```

When in doubt, use the Python helper — it always works.
