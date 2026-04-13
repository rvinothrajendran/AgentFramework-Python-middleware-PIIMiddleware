from typing import Protocol, runtime_checkable, Tuple


@runtime_checkable
class TranslationService(Protocol):

    async def detect_language(self, text: str) -> Tuple[str, float]:
        ...

    async def translate(self, text: str, source: str, target: str) -> str:
        ...
