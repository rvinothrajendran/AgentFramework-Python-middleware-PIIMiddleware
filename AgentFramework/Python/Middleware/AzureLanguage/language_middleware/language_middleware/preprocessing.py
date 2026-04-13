import logging
from agent_framework import Agent, ChatMiddleware
from typing import Awaitable, Callable, Optional
from .config import AzureTranslatorConfig
from .azure_translation import AzureTranslationService
from .llm_translation import LLMTranslationService
from .translation_service import TranslationService
from .builder import _LanguageMiddlewareBuilder

logger = logging.getLogger(__name__)


class LanguageTranslationMiddleware(ChatMiddleware):
    """
    Middleware that translates messages into a target language.
    Uses AzureTranslationService when configured; falls back to
    LLMTranslationService otherwise. After the agent responds,
    the reply is back-translated to the user's original language.
    """

    def __init__(
        self,
        *,
        target_language: str,
        min_confidence: float,
        azure_config: Optional[AzureTranslatorConfig] = None,
        llm_agent=None,
    ):
        self.target_language = target_language
        self.min_confidence = min_confidence
        self.azure = AzureTranslationService(azure_config) if azure_config else None
        self.llm = LLMTranslationService(llm_agent) if llm_agent else None

    async def aclose(self) -> None:
        """Release resources held by translation services (e.g. HTTP sessions)."""
        if self.azure:
            await self.azure.aclose()

    @property
    def _service(self) -> Optional[TranslationService]:
        return self.azure or self.llm

    @classmethod
    def create(
        cls,
        *,
        azure_config: Optional[AzureTranslatorConfig] = None,
        target_language: str = "en",
        min_confidence: float = 0.8,
        llm_agent: Agent = None,
    ):
        return (
            _LanguageMiddlewareBuilder()
            .azure_config(azure_config)
            .target_language(target_language)
            .min_confidence(min_confidence)
            .llm_agent(llm_agent)
            .build()
        )

    async def process(self, context, call_next: Callable[[], Awaitable[None]]):

        service = self._service
        if not service:
            await call_next()
            return

        user_language = None

        # Only the last user message determines the user's language.
        # Earlier history messages are already in the target language.
        last_message = context.messages[-1] if context.messages else None

        if last_message and last_message.text:
            original_text = last_message.text.strip()
            logger.debug("User message length: %d", len(original_text))
            if original_text:
                try:
                    detected_language, confidence = await service.detect_language(original_text)
                except Exception as e:
                    logger.warning("Primary service detect_language failed: %s — trying LLM fallback", e)
                    if self.llm and service is not self.llm:
                        detected_language, confidence = await self.llm.detect_language(original_text)
                    else:
                        await call_next()
                        return

                confidence_meets_threshold = (
                    confidence is None or confidence >= self.min_confidence
                )

                if (
                    detected_language != self.target_language
                    and confidence_meets_threshold
                ):
                    translated = await self._translate(service, last_message, original_text, detected_language, confidence)
                    if translated:
                        last_message.contents[0].text = translated
                        user_language = detected_language  # only back-translate if forward translation succeeded
                elif detected_language != self.target_language:
                    logger.info(
                        "Skipping translation for detected language '%s' due to low confidence (%s < %s)",
                        detected_language,
                        confidence,
                        self.min_confidence,
                    )

        await call_next()

        if context.result:
            for msg in context.result.messages:
                if msg.text:
                    logger.debug("LLM response length: %d", len(msg.text.strip()))

        if user_language and context.result:
            for response_message in context.result.messages:
                if not response_message.text:
                    continue
                response_text = response_message.text.strip()
                if not response_text:
                    continue
                try:
                    back_translated = await service.translate(response_text, self.target_language, user_language)
                    if back_translated:
                        response_message.contents[0].text = back_translated
                        logger.debug("Back-translated response to '%s' (length: %d)", user_language, len(back_translated))
                except Exception as e:
                    logger.warning("Azure back-translation to %s failed: %s — trying LLM fallback", user_language, e)
                    if self.llm and service is not self.llm:
                        try:
                            back_translated = await self.llm.translate(response_text, self.target_language, user_language)
                            if back_translated:
                                response_message.contents[0].text = back_translated
                                logger.debug("Back-translated response to '%s' via LLM (length: %d)", user_language, len(back_translated))
                        except Exception as llm_e:
                            logger.warning("LLM back-translation to %s also failed: %s", user_language, llm_e)

    async def _translate(
        self,
        service: TranslationService,
        message,
        original_text: str,
        detected_language: str,
        confidence: float,
    ) -> Optional[str]:

        if self.azure:
            message.additional_properties["language_detection"] = {
                "detected_language": detected_language,
                "confidence": confidence,
                "target_language": self.target_language,
            }

        if detected_language == self.target_language:
            if self.azure:
                message.additional_properties["language_detection"]["translated"] = False
            return None

        translated_text = None

        try:
            translated_text = await service.translate(original_text, detected_language, self.target_language)
        except Exception as e:
            logger.warning("Translation failed: %s", e)
            if self.llm and service is not self.llm:
                translated_text = await self.llm.translate(original_text, detected_language, self.target_language)

        if self.azure:
            if translated_text:
                message.additional_properties["language_detection"]["translated"] = True
                message.additional_properties["language_detection"]["original_text"] = original_text
            else:
                message.additional_properties["language_detection"]["translated"] = False

        return translated_text

