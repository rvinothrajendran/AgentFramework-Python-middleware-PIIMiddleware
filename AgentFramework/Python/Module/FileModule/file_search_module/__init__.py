"""
azureaicommunity-agent-file-search
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
File search tools for AI agent pipelines — search files by name (glob) or content.

Public API::

    from file_search_module import (
        file_search_by_name,
        file_search_by_content,
        SearchConfig,
        configure,
        get_config,
    )
"""

from .search_config import SearchConfig, configure, get_config
from .file_search_tools import file_search_by_name, file_search_by_content

__all__ = [
    "SearchConfig",
    "configure",
    "get_config",
    "file_search_by_name",
    "file_search_by_content",
]
