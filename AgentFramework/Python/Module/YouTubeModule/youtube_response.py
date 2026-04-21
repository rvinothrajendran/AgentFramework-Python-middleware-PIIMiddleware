from __future__ import annotations

from dataclasses import dataclass


@dataclass
class YouTubeResponse:
    """Represents a single YouTube video search result."""

    title: str = ""
    """Title of the video."""

    description: str = ""
    """Short description of the video."""

    video_url: str = ""
    """Full watch URL, e.g. https://www.youtube.com/watch?v=VIDEO_ID."""

    def __str__(self) -> str:
        return (
            f"Title: {self.title}\n"
            f"Description: {self.description}\n"
            f"URL: {self.video_url}"
        )
