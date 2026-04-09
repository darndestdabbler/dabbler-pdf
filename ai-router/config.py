"""Load and validate router-config.yaml."""

import os
import yaml
from pathlib import Path

# Default config location is router-config.yaml in the same directory as this file.
# This keeps the default working regardless of where Python is invoked from.
_THIS_DIR = Path(__file__).parent


def load_config(path: str | None = None) -> dict:
    if path is None:
        path = str(_THIS_DIR / "router-config.yaml")
    config_path = Path(path)
    if not config_path.exists():
        raise FileNotFoundError(
            f"Router config not found: {path}\n"
            f"Create it from the template in the AI Router specification."
        )

    with open(config_path) as f:
        config = yaml.safe_load(f)

    # Validate API keys exist in environment
    for name, provider in config["providers"].items():
        env_var = provider["api_key_env"]
        if not os.environ.get(env_var):
            raise EnvironmentError(
                f"Missing environment variable {env_var} "
                f"for provider '{name}'. "
                f"Set it with: export {env_var}=your-key-here"
            )

    # Validate model references
    for tier, model_name in config["routing"]["tier_assignments"].items():
        if model_name not in config["models"]:
            raise ValueError(
                f"Tier {tier} references unknown model '{model_name}'. "
                f"Available: {list(config['models'].keys())}"
            )

    # Resolve prompt template file paths relative to config file location
    config_dir = config_path.parent
    for model_name, model_cfg in config["models"].items():
        prompt_file = model_cfg.get("system_prompt_file")
        if prompt_file:
            full_path = config_dir / prompt_file
            if full_path.exists():
                model_cfg["_system_prompt"] = full_path.read_text().strip()
            else:
                model_cfg["_system_prompt"] = (
                    "You are an expert software engineer. "
                    "Be direct and precise."
                )

    # Resolve task-type template paths.
    # "prompt-templates/" is a sibling of config_dir (i.e. one level up when config
    # lives inside ai-router/).  Fall back to config_dir itself if a non-standard
    # layout puts them side-by-side.
    _pt_root = config_dir.parent / "prompt-templates"
    if not _pt_root.exists():
        _pt_root = config_dir / "prompt-templates"
    template_dir = _pt_root / "task-types"
    config["_task_templates"] = {}
    if template_dir.exists():
        for f in template_dir.glob("*.md"):
            task_type = f.stem
            config["_task_templates"][task_type] = f.read_text().strip()

    # Resolve verification template
    v_config = config.get("verification", {})
    v_template_file = (v_config.get("settings", {})
                       .get("prompt_template_file"))
    if v_template_file:
        v_path = config_dir / v_template_file
        if v_path.exists():
            config["_verification_template"] = v_path.read_text().strip()
        else:
            config["_verification_template"] = ""
    else:
        config["_verification_template"] = ""

    return config
