from __future__ import annotations

import copy
from dataclasses import dataclass, field
from typing import List, Optional

__all__ = [
    "SearchConfig",
    "configure",
    "get_config",
]


@dataclass
class SearchConfig:
    """Runtime configuration for the file-search tools.

    All fields have sensible defaults; override only what you need.

    Per-call example::

        result = file_search_by_name("*.py", config=SearchConfig(max_results=50))

    Global default example::

        from search_config import configure
        configure(max_results=50, skip_hidden=True)
    """

    max_results: int = 200
    """Maximum number of paths returned by any single search call."""

    max_file_size_bytes: int = 10 * 1024 * 1024
    """Files larger than this are skipped during content search (default 10 MB)."""

    max_depth: int = 20
    """Maximum directory depth to recurse into (prevents runaway traversal)."""

    binary_check_bytes: int = 8192
    """Number of bytes read to detect binary files (null-byte probe)."""

    follow_symlinks: bool = False
    """Whether to follow symbolic links while walking the directory tree."""

    skip_hidden: bool = False
    """Skip files and directories whose names start with a dot."""

    include_extensions: Optional[List[str]] = None
    """Whitelist of file extensions (e.g. ['.py', '.txt']). None means all."""

    exclude_extensions: Optional[List[str]] = None
    """Blacklist of file extensions (e.g. ['.log', '.tmp'])."""

    encodings: List[str] = field(default_factory=lambda: ["utf-8", "latin-1"])
    """Encoding fallback chain used when reading files for content search."""


# Module-level default — mutated by configure()
_default_config: SearchConfig = SearchConfig()


def configure(**kwargs) -> None:
    """Update the module-level default :class:`SearchConfig`.

    Keyword arguments must match :class:`SearchConfig` field names.

    Example::

        configure(max_results=100, max_depth=5, skip_hidden=True,
                  exclude_extensions=[".log", ".tmp"])
    """
    for key, value in kwargs.items():
        if not hasattr(_default_config, key):
            raise AttributeError(
                f"SearchConfig has no field {key!r}. "
                f"Valid fields: {list(_default_config.__dataclass_fields__)}"
            )
        setattr(_default_config, key, value)


def get_config() -> SearchConfig:
    """Return a snapshot copy of the current default :class:`SearchConfig`."""
    return copy.copy(_default_config)
