from __future__ import annotations

import os

from search_config import SearchConfig

__all__ = [
    "_validate_search_inputs",
    "_is_binary",
    "_extension_allowed",
    "_prune_hidden",
]


def _validate_search_inputs(query: str, path: str) -> str:
    """Validate inputs and return the resolved absolute root path."""
    if not isinstance(query, str) or not query.strip():
        raise ValueError("query must be a non-empty string.")
    if not isinstance(path, str) or not path.strip():
        raise ValueError("path must be a non-empty string.")

    root = os.path.realpath(os.path.abspath(path))
    if not os.path.exists(root):
        raise FileNotFoundError(f"Search path does not exist: {path!r}")
    if not os.path.isdir(root):
        raise NotADirectoryError(f"Search path is not a directory: {path!r}")
    return root


def _is_binary(filepath: str, check_bytes: int) -> bool:
    """Return True if the file appears to be binary (contains null bytes)."""
    try:
        with open(filepath, "rb") as fh:
            return b"\x00" in fh.read(check_bytes)
    except OSError:
        return True


def _extension_allowed(filename: str, cfg: SearchConfig) -> bool:
    """Return False if the file's extension is excluded or not in the whitelist."""
    ext = os.path.splitext(filename)[1].lower()
    if cfg.exclude_extensions and ext in [e.lower() for e in cfg.exclude_extensions]:
        return False
    if cfg.include_extensions is not None and ext not in [e.lower() for e in cfg.include_extensions]:
        return False
    return True


def _prune_hidden(dirnames: list, cfg: SearchConfig) -> None:
    """Remove hidden directories in-place when skip_hidden is enabled."""
    if cfg.skip_hidden:
        dirnames[:] = [d for d in dirnames if not d.startswith(".")]
