from __future__ import annotations

import logging
from dataclasses import dataclass


@dataclass
class YouTubeConfig:
    """Holds all configuration settings required to connect to and query the YouTube Data API v3."""

    api_key: str
    """YouTube Data API v3 key used to authenticate requests."""

    channel_id: str = ""
    """Optional YouTube channel ID to restrict search results to a specific channel."""

    max_results: int = 25
    """Upper bound on the number of results the API may return per request."""

    default_count: int = 10
    """Default number of videos to return when the caller does not specify a count."""

    logger: logging.Logger | None = None
    """Optional logger instance. Falls back to logging.getLogger(__name__) when not provided."""
