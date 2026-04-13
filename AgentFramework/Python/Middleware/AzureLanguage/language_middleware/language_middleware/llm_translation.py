import re
from typing import Tuple
from agent_framework import Agent

_LANG_NAMES = {
    "af": "Afrikaans", "ar": "Arabic", "bg": "Bulgarian", "bn": "Bengali",
    "ca": "Catalan", "cs": "Czech", "cy": "Welsh", "da": "Danish",
    "de": "German", "el": "Greek", "en": "English", "es": "Spanish",
    "et": "Estonian", "fa": "Persian", "fi": "Finnish", "fr": "French",
    "gu": "Gujarati", "he": "Hebrew", "hi": "Hindi", "hr": "Croatian",
    "hu": "Hungarian", "hy": "Armenian", "id": "Indonesian", "is": "Icelandic",
    "it": "Italian", "ja": "Japanese", "ka": "Georgian", "kn": "Kannada",
    "ko": "Korean", "lt": "Lithuanian", "lv": "Latvian", "mk": "Macedonian",
    "ml": "Malayalam", "mr": "Marathi", "ms": "Malay", "mt": "Maltese",
    "nl": "Dutch", "no": "Norwegian", "pa": "Punjabi", "pl": "Polish",
    "pt": "Portuguese", "ro": "Romanian", "ru": "Russian", "sk": "Slovak",
    "sl": "Slovenian", "sq": "Albanian", "sr": "Serbian", "sv": "Swedish",
    "sw": "Swahili", "ta": "Tamil", "te": "Telugu", "th": "Thai",
    "tl": "Filipino", "tr": "Turkish", "uk": "Ukrainian", "ur": "Urdu",
    "vi": "Vietnamese", "zh": "Chinese",
}


def _lang_name(code: str) -> str:
    return _LANG_NAMES.get(code.lower(), code)


class LLMTranslationService:
    """Handles language detection and translation via an LLM agent."""

    def __init__(self, agent: Agent):
        self.agent = agent

    async def detect_language(self, text: str) -> Tuple[str, float]:

        prompt = (
            f"Identify the language of the following text. "
            f"Return only the ISO 639-1 language code (e.g. en, es, fr, de) with no explanations.\n\n"
            f"Example:\n"
            f"Input: Hallo\n"
            f"Output: de\n\n"
            f"Input: {text}\n"
            f"Output:"
        )
        result = await self.agent.run(prompt)
        raw = str(result).strip().lower()
        matches = re.findall(r'\b([a-z]{2,3})\b', raw)
        lang = matches[-1] if matches else "unknown"

        return lang, 1.0

    async def translate(self, text: str, source: str, target: str) -> str:
        source_name = _lang_name(source)
        target_name = _lang_name(target)

        prompt = (
            f"Translate the following text from {source_name} to {target_name}. "
            f"Return only the translated text with no explanations, labels, or extra words.\n\n"
            f"Example:\n"
            f"Input: Hallo\n"
            f"Output: Hello\n\n"
            f"Input: {text}\n"
            f"Output:"
        )
        result = await self.agent.run(prompt)

        return str(result).strip()
