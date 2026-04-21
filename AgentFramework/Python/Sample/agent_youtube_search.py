"""
agent_youtube_search.py — Demonstrates youtube_search_module with an Ollama-backed agent.

The agent has one tool wired in:
  • search_youtube_videos — search YouTube videos by natural language query

Set your YouTube Data API v3 key via the YOUTUBE_API_KEY environment variable.
Optionally set YOUTUBE_CHANNEL_ID to restrict searches to a specific channel.

Run:
    cd Sample
    python agent_youtube_search.py
"""

import asyncio
import logging
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "Module", "YouTubeModule"))

from agent_framework.ollama import OllamaChatClient
from youtube_search_module import YouTubeConfig, YouTubeTools  # type: ignore[import-untyped]

logging.basicConfig(level=logging.INFO, format="%(levelname)s - %(name)s - %(message)s")

# ---------------------------------------------------------------------------
# 1. Configuration — set your YouTube Data API v3 key via environment variable
# ---------------------------------------------------------------------------
YOUTUBE_API_KEY = os.environ.get("YOUTUBE_API_KEY", "")
CHANNEL_ID = os.environ.get("YOUTUBE_CHANNEL_ID", "")  # optional: restrict to a channel

config = YouTubeConfig(
    api_key=YOUTUBE_API_KEY,
    channel_id=CHANNEL_ID,
    max_results=25,
    default_count=5,
)

tools = YouTubeTools.create(config)

# ---------------------------------------------------------------------------
# 2. Agent instructions
# ---------------------------------------------------------------------------
INSTRUCTIONS = """
You are a helpful YouTube search assistant.
When the user asks to find or search for videos, 
Present each result on its own block with title, description, and watch URL.
"""

# ---------------------------------------------------------------------------
# 3. Multi-turn agent
# ---------------------------------------------------------------------------

class YouTubeSearchAgent:
    def __init__(self, model: str = "llama3.2"):
        self.agent = OllamaChatClient(model=model).as_agent(
            name="YouTubeAgent",
            instructions=INSTRUCTIONS,
            tools=tools,
        )
        self.session = self.agent.create_session()

    async def ask(self, message: str) -> str:
        response = await self.agent.run(message, session=self.session)
        return response.text


async def main():
    agent = YouTubeSearchAgent(model="llama3.2")

    queries = [
        "Find Python tutorial videos for beginners",
        "Search for AI agent framework videos",
    ]

    for query in queries:
        print(f"\nUser : {query}")
        result = await agent.ask(query)
        print(f"Agent: {result}")


asyncio.run(main())
