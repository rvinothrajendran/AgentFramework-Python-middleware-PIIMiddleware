from __future__ import annotations

from agent_framework import tool

from .youtube_config import YouTubeConfig
from .youtube_search import YouTubeSearch

__all__ = [
    "YouTubeConfig",
    "YouTubeSearch",
    "YouTubeTools",
]


class YouTubeTools:
    """
    Entry point for integrating YouTube search into an agent pipeline.

    Call ``YouTubeTools.create(config)`` to get a list of ``@tool``-decorated
    functions ready to pass to any agent::

        config = YouTubeConfig(api_key="YOUR_KEY")
        tools  = YouTubeTools.create(config)

        agent = client.as_agent(
            name="YouTubeAgent",
            instructions="You are a helpful assistant with YouTube search.",
            tools=tools,
        )
    """

    @staticmethod
    def create(config: YouTubeConfig) -> list:
        """
        Build and return the list of agent tools backed by *config*.

        Args:
            config: :class:`YouTubeConfig` supplying API credentials and search defaults.

        Returns:
            A list containing the ``search_youtube_videos`` tool function.

        Raises:
            ValueError: If config is None or api_key is empty.
        """
        if config is None:
            raise ValueError("config must not be None.")
        if not config.api_key:
            raise ValueError("YouTubeConfig.api_key must not be empty.")

        searcher = YouTubeSearch(config)

        @tool
        async def search_youtube_videos(
            query: str,
            count: int = 10,
            offset: int = 0,
        ) -> str:
            """Search YouTube videos and return matching video titles, descriptions, and URLs.

            Args:
                query:  Natural language search query for YouTube videos.
                count:  Number of videos to return. Uses config default when <= 0.
                offset: Number of matched videos to skip before returning results.

            Returns:
                A formatted string of matching video results, each with title,
                description, and watch URL. Returns "No results found." when empty.
            """
            results = await searcher.search_async(query, count, offset)
            if not results:
                return "No results found."
            return "\n\n".join(str(r) for r in results)

        return [search_youtube_videos]
