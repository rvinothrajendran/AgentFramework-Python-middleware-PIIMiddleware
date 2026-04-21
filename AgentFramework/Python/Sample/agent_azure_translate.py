
import asyncio
import logging
import os

logging.basicConfig(level=logging.INFO, format="%(levelname)s - %(name)s - %(message)s")

from agent_framework.ollama import OllamaChatClient
from agent_framework import Agent

from language_middleware import LanguageTranslationMiddleware, AzureTranslatorConfig

# Configure Azure Translator
azure_config = AzureTranslatorConfig(
    key=os.environ.get("AZURE_TRANSLATOR_KEY", ""),
    region=os.environ.get("AZURE_TRANSLATOR_REGION", ""),
    endpoint=os.environ.get("AZURE_TRANSLATOR_ENDPOINT", ""),
)


async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    # Build language middleware — detects language and translates to English
    middleware = LanguageTranslationMiddleware.create(
        azure_config=azure_config,
        target_language="en",
        min_confidence=0.8,
        llm_agent=agent,  # LLM fallback when Azure is unavailable
    )

    try:
        # Query in Tamil — middleware will translate it to English and back
        tamil_query = "தஞ்சாவூரின் வரலாற்று சிறப்பு என்ன?"
        tamil_result = await agent.run(tamil_query, middleware=middleware)
        print("Tamil response:", tamil_result.text)

        # Query in German — middleware will translate it to English and back
        german_query = "Was ist die historische Bedeutung von Thanjavur?"
        german_result = await agent.run(german_query, middleware=middleware)
        print("German response:", german_result.text)

    finally:
        await middleware.aclose()


asyncio.run(main())
