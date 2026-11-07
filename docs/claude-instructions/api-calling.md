# API Calling — Claude Code Instructions

This file tells Claude Code how to make Issue Tracker API calls.
For endpoint definitions and payload schemas, see `shared-instructions/api-reference.md`.
All constants (API URL, project ID, actor IDs, session ID) are in
`shared-instructions/project-config.json` — you do not need to pass them manually.

---

## Preferred Method: Python Helper Script

Use the shared helper at `docs/shared-instructions/tracker-api-helpers.py`.
It reads `project-config.json` for all constants and handles JSON serialization.

### First-Session Setup

```bash
# Register the project (writes projectId and projectName to config)
python docs/shared-instructions/tracker-api-helpers.py init --name "openpdf-dotnet"
```

### Session Start

```bash
# Create a session (writes currentClaudeSessionId to config)
python docs/shared-instructions/tracker-api-helpers.py session \
    --agent claude --name "Session NNN — Description"

# Retrieve open issues (projectId read from config)
python docs/shared-instructions/tracker-api-helpers.py get posts --status Open

# Retrieve pending review issues
python docs/shared-instructions/tracker-api-helpers.py get posts --status "Pending Review"

# Read the latest BATON
python docs/shared-instructions/tracker-api-helpers.py baton
```

### Creating Issues and Posts

```bash
# Short text — inline
python docs/shared-instructions/tracker-api-helpers.py post --agent claude --action New \
    --title "Issue title" \
    --tags "tag1,tag2" \
    --text "Description."

# Markdown body — write to temp file first
cat > /tmp/finding.md << 'EOF'
Description with **markdown**.
EOF

python docs/shared-instructions/tracker-api-helpers.py post-file --agent claude --action New \
    --title "Issue title" \
    --tags "tag1,tag2" \
    --text-file /tmp/finding.md
```

### Resolve, Discuss, Archive

```bash
# Resolve an issue (route back to Gemini for IV&V)
python docs/shared-instructions/tracker-api-helpers.py post --agent claude \
    --action Resolve --for 790 --to gemini \
    --text "Fixed. Details of the change."

# Discuss (add commentary)
python docs/shared-instructions/tracker-api-helpers.py post --agent claude \
    --action Discuss --for 790 \
    --text "Commentary."

# Request human review
python docs/shared-instructions/tracker-api-helpers.py post --agent claude \
    --action Check --for 790 --to human \
    --text "Design tradeoff — flagging for human decision."
```

### Posting a BATON

```bash
cat > /tmp/baton-content.md << 'BATONEOF'
# BATON: Session NNN — Title

**Project:** openpdf-dotnet
...full markdown content...
BATONEOF

python docs/shared-instructions/tracker-api-helpers.py post-file --agent claude \
    --action New --title "BATON NNN — Title" --tags "baton" \
    --text-file /tmp/baton-content.md
```

### Queries

```bash
python docs/shared-instructions/tracker-api-helpers.py get posts --status Open
python docs/shared-instructions/tracker-api-helpers.py get posts/790/thread
python docs/shared-instructions/tracker-api-helpers.py get posts --status Open --tags "sev:critical"
python docs/shared-instructions/tracker-api-helpers.py config
```

---

## Fallback: Inline Python

If the helper script is unavailable, use inline Python to build the JSON payload
and pipe it to curl:

```bash
python3 -c "
import json, sys
payload = {
    'sessionId': 5,
    'fromActorId': 1,
    'actionType': 'New',
    'title': 'Issue title',
    'tags': 'tag1,tag2',
    'text': sys.stdin.read()
}
print(json.dumps(payload))
" <<'TEXTEOF' | curl -s -X POST http://localhost:5124/api/posts \
    -H 'Content-Type: application/json' -d @-
Markdown body goes here.
TEXTEOF
```
