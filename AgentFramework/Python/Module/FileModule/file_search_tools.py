from __future__ import annotations

import fnmatch
import os
from typing import List, Optional

from agent_framework import tool
from search_config import SearchConfig, configure, get_config, _default_config
from search_helpers import _validate_search_inputs, _is_binary, _extension_allowed, _prune_hidden

__all__ = [
    "SearchConfig",
    "configure",
    "get_config",
    "file_search_by_name",
    "file_search_by_content",
]

# ---------------------------------------------------------------------------
# File search tools
# ---------------------------------------------------------------------------

@tool
def file_search_by_name(
    query: str = "*",
    path: str = ".",
    case_sensitive: bool = False,
    file_types: Optional[List[str]] = None,
    config: Optional[SearchConfig] = None,
) -> list[str]:
    """Search for files whose names match a glob pattern under a directory.

    Args:
        query: Glob pattern to match against file names (e.g. '*.py', 'main*').
            Bare extensions are auto-normalized: 'pdf' and '.pdf' both become '*.pdf'.
            Defaults to '*' (match all files).
        path: Root directory to search in. Defaults to the current directory.
        case_sensitive: Whether the glob match is case-sensitive. Defaults to
            False for consistent cross-platform behaviour.
        file_types: Optional list of extensions to match (e.g. ['.pdf', '.doc']).
            When provided, a file must match BOTH the query glob AND have one of
            these extensions. Pass query='*' to filter by type alone.
        config: Optional :class:`SearchConfig` to override the module default
            for this call only.

    Returns:
        A list of matching file paths (relative to *path*), capped at
        ``config.max_results``.

    Raises:
        ValueError: If query or path are invalid.
        FileNotFoundError: If path does not exist.
        NotADirectoryError: If path is not a directory.
    """
    cfg = config or _default_config
    root = _validate_search_inputs(query, path)

    # Auto-normalize bare extensions: 'pdf' -> '*.pdf', '.pdf' -> '*.pdf'
    q = query.strip()
    if q and not any(c in q for c in ("*", "?", "[")):
        if q.startswith("."):
            q = f"*{q}"
        else:
            q = f"*.{q}"

    normalise = (lambda s: s) if case_sensitive else str.lower
    pattern = normalise(q)

    # Normalize file_types for fast lookup
    allowed_types: Optional[set[str]] = None
    if file_types:
        allowed_types = {
            (t.lower() if t.startswith(".") else f".{t.lower()}")
            for t in file_types
        }

    matches: list[str] = []
    seen_dirs: set[str] = set()

    for dirpath, dirnames, filenames in os.walk(
        root, onerror=None, followlinks=cfg.follow_symlinks
    ):
        # Symlink-loop guard
        real_dir = os.path.realpath(dirpath)
        if real_dir in seen_dirs:
            dirnames.clear()
            continue
        seen_dirs.add(real_dir)

        # Depth guard
        depth = dirpath[len(root):].count(os.sep)
        if depth >= cfg.max_depth:
            dirnames.clear()
            continue

        _prune_hidden(dirnames, cfg)

        for filename in filenames:
            if cfg.skip_hidden and filename.startswith("."):
                continue
            if not _extension_allowed(filename, cfg):
                continue
            if allowed_types is not None:
                ext = os.path.splitext(filename)[1].lower()
                if ext not in allowed_types:
                    continue
            if fnmatch.fnmatch(normalise(filename), pattern):
                rel = os.path.relpath(os.path.join(dirpath, filename), root)
                matches.append(rel)
                if len(matches) >= cfg.max_results:
                    return matches

    return matches


@tool
def file_search_by_content(
    query: str,
    path: str = ".",
    case_sensitive: bool = True,
    file_types: Optional[List[str]] = None,
    config: Optional[SearchConfig] = None,
) -> list[str]:
    """Search for files whose contents contain a given string.

    Args:
        query: Plain-text string to search for inside files.
        path: Root directory to search in. Defaults to the current directory.
        case_sensitive: Whether the string match is case-sensitive.
            Defaults to True.
        file_types: Optional list of extensions to restrict the search to
            (e.g. ['.py', '.txt']). Accepts with or without leading dot.
            When provided, only files with a matching extension are opened.
        config: Optional :class:`SearchConfig` to override the module default
            for this call only.

    Returns:
        A list of file paths (relative to *path*) that contain the query
        string, capped at ``config.max_results``.

    Raises:
        ValueError: If query or path are invalid.
        FileNotFoundError: If path does not exist.
        NotADirectoryError: If path is not a directory.
    """
    cfg = config or _default_config
    root = _validate_search_inputs(query, path)
    needle = query.strip() if case_sensitive else query.strip().lower()

    # Normalize file_types for fast lookup
    allowed_types: Optional[set[str]] = None
    if file_types:
        allowed_types = {
            (t.lower() if t.startswith(".") else f".{t.lower()}")
            for t in file_types
        }

    matches: list[str] = []
    seen_dirs: set[str] = set()

    for dirpath, dirnames, filenames in os.walk(
        root, onerror=None, followlinks=cfg.follow_symlinks
    ):
        # Symlink-loop guard
        real_dir = os.path.realpath(dirpath)
        if real_dir in seen_dirs:
            dirnames.clear()
            continue
        seen_dirs.add(real_dir)

        # Depth guard
        depth = dirpath[len(root):].count(os.sep)
        if depth >= cfg.max_depth:
            dirnames.clear()
            continue

        _prune_hidden(dirnames, cfg)

        for filename in filenames:
            if cfg.skip_hidden and filename.startswith("."):
                continue
            if not _extension_allowed(filename, cfg):
                continue
            if allowed_types is not None:
                ext = os.path.splitext(filename)[1].lower()
                if ext not in allowed_types:
                    continue

            filepath = os.path.join(dirpath, filename)

            try:
                if os.path.getsize(filepath) > cfg.max_file_size_bytes:
                    continue
            except OSError:
                continue

            if _is_binary(filepath, cfg.binary_check_bytes):
                continue

            content: Optional[str] = None
            for encoding in cfg.encodings:
                try:
                    with open(filepath, encoding=encoding, errors="strict") as fh:
                        content = fh.read()
                    break
                except (OSError, UnicodeDecodeError):
                    continue

            if content is None:
                continue

            haystack = content if case_sensitive else content.lower()
            if needle in haystack:
                rel = os.path.relpath(filepath, root)
                matches.append(rel)
                if len(matches) >= cfg.max_results:
                    return matches

    return matches