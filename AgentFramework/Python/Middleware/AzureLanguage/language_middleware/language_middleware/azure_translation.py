import asyncio
from typing import Tuple
from azure.ai.translation.text import TextTranslationClient
from azure.core.credentials import AzureKeyCredential
from azure.core.exceptions import HttpResponseError
from .config import AzureTranslatorConfig


class AzureTranslationService:
    """Handles language detection and translation via the Azure AI Translation Text SDK."""

    def __init__(self, config: AzureTranslatorConfig):
        self.config = config
        credential = AzureKeyCredential(config.key)
        self._client = TextTranslationClient(
            credential=credential,
            region=config.region,
            endpoint=config.endpoint,
        )

    async def aclose(self) -> None:
        self._client.close()

    async def detect_language(self, text: str) -> Tuple[str, float]:
        try:
            response = await asyncio.get_event_loop().run_in_executor(
                None,
                lambda: self._client.translate(body=[text], to_language=["en"])
            )
            item = response[0] if response else None
            if item and item.detected_language:
                return item.detected_language.language, float(item.detected_language.score)
            raise RuntimeError("Azure detect_language: no detection result returned")
        except HttpResponseError as e:
            raise RuntimeError(f"Azure detect_language failed ({e.status_code}): {e.message}") from e

    async def translate(self, text: str, source: str, target: str) -> str:
        try:
            response = await asyncio.get_event_loop().run_in_executor(
                None,
                lambda: self._client.translate(body=[text], from_language=source, to_language=[target])
            )
            item = response[0] if response else None
            if item and item.translations:
                return item.translations[0].text
            raise RuntimeError("Azure translate: no translation result returned")
        except HttpResponseError as e:
            raise RuntimeError(f"Azure translate failed ({e.status_code}): {e.message}") from e
