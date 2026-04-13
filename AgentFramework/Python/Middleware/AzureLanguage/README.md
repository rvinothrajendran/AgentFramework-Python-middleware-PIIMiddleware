<div align="center">

# 🌐 AzureAICommunity - Agent - Language Middleware

Automatic language detection, translation, and response back-translation middleware for AI agent applications built on the **Agent Framework**.

[![PyPI Version](https://img.shields.io/pypi/v/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/)
[![Python Versions](https://img.shields.io/pypi/pyversions/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/)
![PyPI Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-language-middleware)
[![License](https://img.shields.io/pypi/l/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/)
[![PyPI Status](https://img.shields.io/pypi/status/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/)



**Your agent always reasons in its best language — your users always receive replies in theirs.**

[Getting Started](#-installation) · [Configuration](#️-configuration) · [Usage](#-usage) · [Contributing](#-contributing)

</div>

---

## Overview

`azureaicommunity-agent-language-middleware` is a plug-and-play translation layer for AI agent pipelines built on `agent-framework`. It automatically detects the user's language, translates incoming messages to the agent's target language, and back-translates the response — using Azure Translator with an optional LLM agent as fallback.

<img src="assets/language.png" alt="How It Works" height="600"/>

---

## ✨ Features

| | Feature |
|---|---|
| 🔍 | **Language detection** — automatically detect the user's language with configurable confidence threshold |
| 🔄 | **Round-trip translation** — translate incoming messages and back-translate agent responses |
| ☁️ | **Azure Translator** — production-grade speed and accuracy with 100+ languages |
| 🤖 | **LLM fallback** — silently falls back to LLM translation when Azure is unavailable |
| 🔧 | **Builder pattern** — fluent `create()` API to compose and customize middleware |
| 🔌 | **Framework integration** — drops directly into `agent-framework` middleware pipelines |

---

## 📦 Installation

```bash
pip install azureaicommunity-agent-language-middleware
```

---

## 🚀 Quick Start

```python
import asyncio
from agent_framework import Agent
from agent_framework.ollama import OllamaChatClient
from language_middleware import LanguageTranslationMiddleware

async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    middleware = LanguageTranslationMiddleware.create(
        llm_agent=agent,
        target_language="en",
    )

    try:
        result = await agent.run("Wie heißt die Hauptstadt von Frankreich?", middleware=middleware)
        print(result.text)  # "Die Hauptstadt von Frankreich ist Paris."
    finally:
        await middleware.aclose()

asyncio.run(main())
```

---

## ⚙️ Configuration

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `target_language` | `str` | `"en"` | ISO 639-1 code of the language the agent should reason in |
| `min_confidence` | `float` | `0.8` | Minimum detection confidence to skip translation |
| `azure_config` | `AzureTranslatorConfig` | `None` | Azure Translator credentials (optional) |
| `llm_agent` | `Agent` | `None` | LLM agent used as translation fallback or primary service |

---

## 🧑‍💻 Usage

### Option 1 — Azure Translator + LLM Fallback (Recommended)

```python
import asyncio
import os
from agent_framework import Agent
from agent_framework.ollama import OllamaChatClient
from language_middleware import LanguageTranslationMiddleware, AzureTranslatorConfig

azure_config = AzureTranslatorConfig(
    key=os.environ.get("AZURE_TRANSLATOR_KEY", ""),
    region=os.environ.get("AZURE_TRANSLATOR_REGION", ""),
    endpoint=os.environ.get("AZURE_TRANSLATOR_ENDPOINT", ""),
)

async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    middleware = LanguageTranslationMiddleware.create(
        azure_config=azure_config,
        target_language="en",
        min_confidence=0.8,
        llm_agent=agent,
    )

    try:
        tamil_query = "தஞ்சாவூரின் வரலாற்று சிறப்பு என்ன?"
        tamil_result = await agent.run(tamil_query, middleware=middleware)
        print("Tamil response:", tamil_result.text)

        german_query = "Was ist die historische Bedeutung von Thanjavur?"
        german_result = await agent.run(german_query, middleware=middleware)
        print("German response:", german_result.text)
    finally:
        await middleware.aclose()

asyncio.run(main())
```

### Option 2 — LLM-Only (No Azure Subscription Required)

```python
import asyncio
from agent_framework import Agent
from agent_framework.ollama import OllamaChatClient
from language_middleware import LanguageTranslationMiddleware

async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    middleware = LanguageTranslationMiddleware.create(
        llm_agent=agent,
        target_language="en",
    )

    try:
        result = await agent.run("Wie heißt die Hauptstadt von Frankreich?", middleware=middleware)
        print(result.text)  # "Die Hauptstadt von Frankreich ist Paris."
    finally:
        await middleware.aclose()

asyncio.run(main())
```

### Option 3 — Azure Only (No LLM Fallback)

```python
middleware = LanguageTranslationMiddleware.create(
    azure_config=azure_config,
    target_language="en",
)
```

---

## ⚙️ How It Works

```
1. Intercept      →  middleware captures the last user message
2. Detect         →  language is detected with a confidence score
3. Translate      →  message is translated to the target language (if needed)
4. Agent runs     →  the primary LLM processes the translated message
5. Back-translate →  agent response is translated back to the user's original language
6. Return         →  user receives the response in their native language
```

**Translation providers (with automatic fallback):**

| Priority | Provider | Capability |
|----------|----------|------------|
| 1st | Azure Translator | Detect language + translate (fast, accurate, 100+ languages) |
| 2nd | LLM Agent (fallback) | Detect language + translate when Azure is unavailable |

---

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to change before submitting a pull request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 📄 License

MIT — see [LICENSE](LICENSE) for details.
