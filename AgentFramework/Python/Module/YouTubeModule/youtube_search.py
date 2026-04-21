from __future__ import annotations

import logging
from typing import List

import aiohttp

from youtube_config import YouTubeConfig
from youtube_response import YouTubeResponse

_SEARCH_URL = "https://www.googleapis.com/youtube/v3/search"


class YouTubeSearch:
    """
    Calls the YouTube Data API v3 search endpoint and maps results to YouTubeResponse objects.
    """

    def __init__(self, config: YouTubeConfig) -> None:
        if not config.api_key:
            raise ValueError("YouTubeConfig.api_key must not be empty.")
        self._config = config
        self._logger = config.logger or logging.getLogger(__name__)

    async def search_async(
        self,
        query: str,
        count: int = 10,
        offset: int = 0,
    ) -> List[YouTubeResponse]:
        """
        Executes a YouTube search and returns a paged slice of YouTubeResponse objects.

        Args:
            query:  Search keywords to send to the YouTube Data API.
            count:  Maximum number of results to retrieve. Uses config.default_count when <= 0.
            offset: Number of leading results to skip (client-side paging).

        Returns:
            A list of YouTubeResponse items, or an empty list when no results are found.
        """
        if not query:
            raise ValueError("query must be a non-empty string.")

        safe_offset = max(0, offset)
        max_results = max(1, self._config.max_results)
        safe_count = max(1, min(count if count > 0 else self._config.default_count, max_results))
        fetch_count = min(max_results, safe_offset + safe_count)

        params: dict = {
            "part": "snippet",
            "q": query,
            "maxResults": fetch_count,
            "type": "video",
            "key": self._config.api_key,
        }
        if self._config.channel_id:
            params["channelId"] = self._config.channel_id

        self._logger.debug("YouTube search: query=%r, fetch_count=%d", query, fetch_count)

        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    _SEARCH_URL, params=params, timeout=aiohttp.ClientTimeout(total=15)
                ) as resp:
                    resp.raise_for_status()
                    data = await resp.json()
        except Exception as exc:
            self._logger.error("YouTube API call failed: %s", exc)
            return []

        items = data.get("items", [])
        results: List[YouTubeResponse] = []

        for item in items[safe_offset:]:
            video_id = item.get("id", {}).get("videoId")
            if not video_id:
                continue
            snippet = item.get("snippet", {})
            results.append(
                YouTubeResponse(
                    title=snippet.get("title", ""),
                    description=snippet.get("description", ""),
                    video_url=f"https://www.youtube.com/watch?v={video_id}",
                )
            )
            if len(results) >= safe_count:
                break

        return results
