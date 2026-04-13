from typing import Optional
from .config import AzureTranslatorConfig


class _LanguageMiddlewareBuilder:

    def __init__(self):
        self._target_language = "en"
        self._min_confidence = 0.8
        self._azure_config: Optional[AzureTranslatorConfig] = None
        self._llm_agent = None

    def target_language(self, language: str):
        self._target_language = language
        return self

    def min_confidence(self, confidence: float):
        self._min_confidence = confidence
        return self

    def azure_config(self, config: Optional[AzureTranslatorConfig]):
        self._azure_config = config
        return self

    def llm_agent(self, agent):
        self._llm_agent = agent
        return self

    def build(self):
        from .preprocessing import LanguageTranslationMiddleware

        return LanguageTranslationMiddleware(
            target_language=self._target_language,
            min_confidence=self._min_confidence,
            azure_config=self._azure_config,
            llm_agent=self._llm_agent,
        )
