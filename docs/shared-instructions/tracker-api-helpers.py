#!/usr/bin/env python3
"""
Issue Tracker API Helper
========================
A single-file helper for making Issue Tracker REST API calls from any shell
environment. Reads configuration from project-config.json so callers don't
need to pass URLs, actor IDs, or session IDs on every call.

Configuration:
    The script looks for project-config.json in the same directory as itself.
    Override with: TRACKER_CONFIG_PATH=/path/to/project-config.json

Agent identification:
    Pass --agent claude or --agent gemini. The script resolves the actor ID
    and session ID automatically from the config file.

Usage:

    # Create a session (writes session ID back to config)
    python tracker_api.py session --agent claude --name "Session 042 — Keyboard nav"

    # Create an issue
    python tracker_api.py post --agent claude --action New \\
        --title "Issue title" --tags "tag1,tag2" --text "Description"

    # Post with markdown body from a file
    python tracker_api.py post-file --agent claude --action New \\
        --title "BATON 042" --tags "baton" --text-file path/to/baton.md

    # Resolve an IV&V finding (--to accepts agent names)
    python tracker_api.py post --agent claude --action Resolve \\
        --for 790 --to gemini --text "Fixed in commit abc123."

    # Query open issues (projectId from config)
    python tracker_api.py get posts --status Open

    # Get latest BATON
    python tracker_api.py baton

    # First-session project registration
    python tracker_api.py init --name "Dabbler.WpfControls"

    # Show current config
    python tracker_api.py config
"""

import argparse
import json
import os
import sys
import urllib.request
import urllib.error
import urllib.parse

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

def _config_path() -> str:
    """Resolve the path to project-config.json."""
    env = os.environ.get("TRACKER_CONFIG_PATH")
    if env:
        return env
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), "project-config.json")


def load_config() -> dict:
    """Load and return the config dict, or exit with a helpful message."""
    path = _config_path()
    if not os.path.exists(path):
        print(f"Config not found: {path}", file=sys.stderr)
        print("Create project-config.json or set TRACKER_CONFIG_PATH.", file=sys.stderr)
        sys.exit(1)
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def save_config(cfg: dict) -> None:
    """Write the config dict back to project-config.json."""
    path = _config_path()
    with open(path, "w", encoding="utf-8") as f:
        json.dump(cfg, f, indent=2)
        f.write("\n")


AGENT_MAP = {
    "claude": {"actor_key": "actorIdForClaude", "session_key": "currentClaudeSessionId"},
    "gemini": {"actor_key": "actorIdForGemini", "session_key": "currentGeminiSessionId"},
    "human":  {"actor_key": "actorIdForHuman",  "session_key": None},
    "system": {"actor_key": "actorIdForSystem", "session_key": None},
}


def resolve_actor_id(cfg: dict, agent_or_id: str) -> int:
    """Resolve an agent name or numeric ID to an actor ID."""
    if agent_or_id.isdigit():
        return int(agent_or_id)
    agent = agent_or_id.lower()
    if agent in AGENT_MAP:
        return cfg[AGENT_MAP[agent]["actor_key"]]
    print(f"Unknown agent: {agent_or_id}. Use: claude, gemini, human, system, or a numeric ID.", file=sys.stderr)
    sys.exit(1)


def resolve_session_id(cfg: dict, agent: str) -> int:
    """Get the current session ID for an agent from config."""
    agent = agent.lower()
    if agent not in AGENT_MAP or AGENT_MAP[agent]["session_key"] is None:
        print(f"No session tracking for agent '{agent}'.", file=sys.stderr)
        sys.exit(1)
    session_id = cfg.get(AGENT_MAP[agent]["session_key"])
    if session_id is None:
        print(f"No active session for {agent}. Run: tracker_api.py session --agent {agent} --name \"...\"", file=sys.stderr)
        sys.exit(1)
    return session_id


def get_api_url(cfg: dict) -> str:
    """Get the base API URL from config."""
    url = cfg.get("issueTrackerApiUrl")
    if not url:
        print("issueTrackerApiUrl is not set in project-config.json.", file=sys.stderr)
        sys.exit(1)
    return url.rstrip("/")


def get_project_id(cfg: dict) -> int:
    """Get the project ID from config."""
    pid = cfg.get("projectId")
    if pid is None:
        print("projectId is not set. Run: tracker_api.py init --name \"ProjectName\"", file=sys.stderr)
        sys.exit(1)
    return pid


# ---------------------------------------------------------------------------
# HTTP helpers
# ---------------------------------------------------------------------------

def api_post(base_url: str, endpoint: str, payload: dict) -> dict:
    """POST JSON to an API endpoint and return the parsed response."""
    url = f"{base_url}/{endpoint}"
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        url, data=data, headers={"Content-Type": "application/json"}, method="POST"
    )
    try:
        with urllib.request.urlopen(req) as resp:
            body = resp.read().decode("utf-8")
            return json.loads(body) if body.strip() else {}
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        print(f"HTTP {e.code}: {body}", file=sys.stderr)
        sys.exit(1)
    except urllib.error.URLError as e:
        print(f"Connection failed: {e.reason}", file=sys.stderr)
        print(f"Is the Issue Tracker running at {base_url}?", file=sys.stderr)
        sys.exit(1)


def api_get(base_url: str, endpoint: str, params: dict = None) -> dict | list:
    """GET from an API endpoint and return the parsed response."""
    url = f"{base_url}/{endpoint}"
    if params:
        filtered = {k: v for k, v in params.items() if v is not None}
        if filtered:
            url += "?" + urllib.parse.urlencode(filtered)
    req = urllib.request.Request(url, method="GET")
    try:
        with urllib.request.urlopen(req) as resp:
            body = resp.read().decode("utf-8")
            return json.loads(body) if body.strip() else {}
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        print(f"HTTP {e.code}: {body}", file=sys.stderr)
        sys.exit(1)
    except urllib.error.URLError as e:
        print(f"Connection failed: {e.reason}", file=sys.stderr)
        print(f"Is the Issue Tracker running at {base_url}?", file=sys.stderr)
        sys.exit(1)


# ---------------------------------------------------------------------------
# Commands
# ---------------------------------------------------------------------------

def cmd_init(args):
    """Register a new project and save the config."""
    cfg = load_config()
    base_url = get_api_url(cfg)

    payload = {"name": args.name}
    result = api_post(base_url, "projects", payload)
    print(json.dumps(result, indent=2))

    cfg["projectName"] = result.get("name", args.name)
    cfg["projectId"] = result["id"]
    save_config(cfg)
    print(f"\nConfig updated: projectId={result['id']}, projectName={cfg['projectName']}", file=sys.stderr)


def cmd_session(args):
    """Create a new session and save the session ID to config."""
    cfg = load_config()
    base_url = get_api_url(cfg)
    project_id = get_project_id(cfg)

    payload = {"projectId": project_id, "name": args.name}
    result = api_post(base_url, "sessions", payload)
    print(json.dumps(result, indent=2))

    # Write session ID back to config
    agent = args.agent.lower()
    session_key = AGENT_MAP[agent]["session_key"]
    if session_key:
        cfg[session_key] = result["id"]
        save_config(cfg)
        print(f"\nConfig updated: {session_key}={result['id']}", file=sys.stderr)


def cmd_post(args):
    """Create a post (issue, discussion, resolve, archive, etc.)."""
    cfg = load_config()
    base_url = get_api_url(cfg)
    actor_id = resolve_actor_id(cfg, args.agent)
    session_id = args.session if args.session else resolve_session_id(cfg, args.agent)

    payload = {
        "sessionId": session_id,
        "fromActorId": actor_id,
        "actionType": args.action,
    }
    if args.title:
        payload["title"] = args.title
    if args.tags:
        payload["tags"] = args.tags
    if args.for_post:
        payload["actionForId"] = args.for_post
    if args.to:
        payload["toActorId"] = resolve_actor_id(cfg, args.to)

    # Text from --text or --text-file
    text = args.text
    if hasattr(args, "text_file") and args.text_file:
        with open(args.text_file, "r", encoding="utf-8") as f:
            text = f.read()
    if text:
        payload["text"] = text

    result = api_post(base_url, "posts", payload)
    print(json.dumps(result, indent=2))


def cmd_post_file(args):
    """Create a post with text content read from a file."""
    cfg = load_config()
    base_url = get_api_url(cfg)
    actor_id = resolve_actor_id(cfg, args.agent)
    session_id = args.session if args.session else resolve_session_id(cfg, args.agent)

    with open(args.text_file, "r", encoding="utf-8") as f:
        text = f.read()

    payload = {
        "sessionId": session_id,
        "fromActorId": actor_id,
        "actionType": args.action,
        "text": text,
    }
    if args.title:
        payload["title"] = args.title
    if args.tags:
        payload["tags"] = args.tags
    if args.for_post:
        payload["actionForId"] = args.for_post
    if args.to:
        payload["toActorId"] = resolve_actor_id(cfg, args.to)

    result = api_post(base_url, "posts", payload)
    print(json.dumps(result, indent=2))


def cmd_get(args):
    """GET from any API endpoint with optional query parameters."""
    cfg = load_config()
    base_url = get_api_url(cfg)

    params = {}
    if args.status:
        params["status"] = args.status
    if args.project:
        params["projectId"] = args.project
    elif args.endpoint == "posts":
        # Default to the configured project for post queries
        pid = cfg.get("projectId")
        if pid is not None:
            params["projectId"] = pid
    if args.session_id:
        params["sessionId"] = args.session_id
    if args.tags:
        params["tags"] = args.tags

    result = api_get(base_url, args.endpoint, params)
    print(json.dumps(result, indent=2))


def cmd_baton(args):
    """Retrieve the latest BATON for the project."""
    cfg = load_config()
    base_url = get_api_url(cfg)
    project_id = args.project if args.project else get_project_id(cfg)

    result = api_get(base_url, "posts/latest-baton", {"projectId": project_id})
    print(json.dumps(result, indent=2))


def cmd_config(args):
    """Show the current configuration."""
    cfg = load_config()
    print(json.dumps(cfg, indent=2))


# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------

def add_agent_arg(parser, required=True):
    """Add the --agent argument to a parser."""
    parser.add_argument(
        "--agent", required=required,
        help="Agent name: claude, gemini (resolves actor ID and session ID from config)"
    )


def main():
    parser = argparse.ArgumentParser(
        description="Issue Tracker API Helper — reads project-config.json for all constants",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # First-session project registration
  python tracker_api.py init --name "Dabbler.WpfControls"

  # Create a session (saves session ID to config)
  python tracker_api.py session --agent claude --name "Session 042 — Keyboard nav"

  # Create an issue
  python tracker_api.py post --agent claude --action New \\
      --title "[SEV-MAJOR] Bug" --tags "sev:major" --text "Description"

  # Post with markdown body from a file
  python tracker_api.py post-file --agent claude --action New \\
      --title "BATON 042" --tags "baton" --text-file /tmp/baton.md

  # Resolve an IV&V finding (--to accepts agent names)
  python tracker_api.py post --agent claude --action Resolve \\
      --for 790 --to gemini --text "Fixed."

  # Query open issues (projectId from config)
  python tracker_api.py get posts --status Open

  # Get a thread
  python tracker_api.py get posts/790/thread

  # Get latest BATON (projectId from config)
  python tracker_api.py baton

  # Show current config
  python tracker_api.py config
        """,
    )
    sub = parser.add_subparsers(dest="command", required=True)

    # --- init ---
    p_init = sub.add_parser("init", help="Register a new project (first session)")
    p_init.add_argument("--name", required=True, help="Project name")
    p_init.set_defaults(func=cmd_init)

    # --- session ---
    p_sess = sub.add_parser("session", help="Create a session (saves ID to config)")
    add_agent_arg(p_sess)
    p_sess.add_argument("--name", required=True, help="Session name")
    p_sess.set_defaults(func=cmd_session)

    # --- post ---
    p_post = sub.add_parser("post", help="Create a post")
    add_agent_arg(p_post)
    p_post.add_argument("--action", required=True, help="ActionType (New, Discuss, Resolve, Archive, Hold, Reopen, Check)")
    p_post.add_argument("--title", help="Post title (required for New)")
    p_post.add_argument("--tags", help="Comma-separated tags")
    p_post.add_argument("--for", type=int, dest="for_post", help="actionForId (parent post)")
    p_post.add_argument("--to", help="Reviewer: agent name (claude, gemini, human) or numeric ID")
    p_post.add_argument("--text", help="Post body text")
    p_post.add_argument("--text-file", help="Read post body from file (overrides --text)")
    p_post.add_argument("--session", type=int, help="Override session ID (default: from config)")
    p_post.set_defaults(func=cmd_post)

    # --- post-file ---
    p_pf = sub.add_parser("post-file", help="Create a post with text from a file")
    add_agent_arg(p_pf)
    p_pf.add_argument("--action", required=True)
    p_pf.add_argument("--title")
    p_pf.add_argument("--tags")
    p_pf.add_argument("--for", type=int, dest="for_post")
    p_pf.add_argument("--to", help="Reviewer: agent name or numeric ID")
    p_pf.add_argument("--text-file", required=True, help="Path to file containing post body")
    p_pf.add_argument("--session", type=int, help="Override session ID")
    p_pf.set_defaults(func=cmd_post_file)

    # --- get ---
    p_get = sub.add_parser("get", help="GET from an API endpoint")
    p_get.add_argument("endpoint", help="API path (e.g., 'posts', 'posts/123/thread')")
    p_get.add_argument("--status", help="Filter by status")
    p_get.add_argument("--project", type=int, help="Override projectId (default: from config)")
    p_get.add_argument("--session-id", type=int, help="Filter by sessionId")
    p_get.add_argument("--tags", help="Filter by tags")
    p_get.set_defaults(func=cmd_get)

    # --- baton ---
    p_baton = sub.add_parser("baton", help="Get latest BATON")
    p_baton.add_argument("--project", type=int, help="Override projectId (default: from config)")
    p_baton.set_defaults(func=cmd_baton)

    # --- config ---
    p_cfg = sub.add_parser("config", help="Show current configuration")
    p_cfg.set_defaults(func=cmd_config)

    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
