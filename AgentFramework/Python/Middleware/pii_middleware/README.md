<div align="center">

# 🛡️ AzureAICommunity - Agent - PII Middleware

Secure AI agent pipelines by detecting and controlling **personally identifiable information (PII)** before it reaches the AI model.

[![PyPI Version](https://img.shields.io/pypi/v/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/)
[![Python Versions](https://img.shields.io/pypi/pyversions/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/)
![PyPI Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-pii-middleware)
[![License](https://img.shields.io/pypi/l/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/)
[![PyPI Status](https://img.shields.io/pypi/status/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/)



**Intercept, detect, and block sensitive personal data before it reaches your LLM — with zero friction.**

[Getting Started](#-installation) · [Profiles](#-security-profiles) · [LLM Validation](#-llm-assisted-validation) · [Contributing](#-contributing)

</div>

---

## Overview

`azureaicommunity-agent-pii-middleware` is a plug-and-play security layer for AI agent pipelines built on `agent-framework`. It scans every user message for PII using Microsoft's [Recognizers Text](https://github.com/microsoft/Recognizers-Text) library and can optionally route ambiguous detections through a secondary LLM for a second opinion.

<img width="650" height="700" alt="image" src="https://github.com/user-attachments/assets/edbc23c5-2d97-4dc7-b6ae-c93d9f2c4721" />



---

## ✨ Features

| | Feature |
|---|---|
| 🔍 | **PII detection** — emails, phones, IPs, credit cards, SSNs, dates, numbers, units |
| 🎛️ | **Profile-based config** — one-line setup with `strict`, `standard`, `financial`, `healthcare` |
| 🔧 | **Builder pattern** — fluent API to compose and customize middleware pipelines |
| 🤖 | **LLM validation** — route edge cases through a secondary agent to reduce false positives |
| 🔌 | **Framework integration** — drops directly into `agent-framework` middleware pipelines |

---

## 📦 Installation

```bash
pip install azureaicommunity-agent-pii-middleware
```

---

## 🚀 Quick Start

```python
import asyncio
from agent_framework.ollama import OllamaChatClient
from agent_framework import Agent
from pii_middleware import PIIMiddleware

# Build a middleware pipeline using the "standard" profile
middleware = (
    PIIMiddleware
        .profile("standard")
        .build()
)

async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    result = await agent.run("My email is user@example.com", middleware=middleware)
    print(result.text)
    # → "Message blocked: sensitive information detected (email)."

asyncio.run(main())
```

---

## 🎛️ Security Profiles

Choose a pre-built profile to get started instantly:

| Profile | Blocked | Allowed |
|---|---|---|
| `strict` | `email` `phone_number` `ip` `credit_card` | `datetime` `number` |
| `standard` | `email` `phone_number` | `datetime` `number` `unit` |
| `financial` | `credit_card` `ssn` `account_number` `email` | `datetime` |
| `healthcare` | `patient_id` `ssn` `email` `phone_number` | `datetime` `unit` |

```python
# Built-in profile
middleware = PIIMiddleware.profile("strict").build()

# Custom profile dict
middleware = (
    PIIMiddleware
        .profile({"block": ["email", "ssn"], "allow": ["datetime"]})
        .build()
)
```

---

## 🔧 Custom Entity Lists

Fine-tune the block/allow lists after applying any profile:

```python
middleware = (
    PIIMiddleware
        .profile("standard")
        .block_entities(["email", "phone_number", "credit_card"])
        .allow_entities(["datetime", "number"])
        .build()
)
```

---

## 🤖 LLM-Assisted Validation

Attach a secondary LLM agent that makes the final allow/block decision when PII is detected:

```python
from agent_framework.ollama import OllamaChatClient
from agent_framework import Agent

validator = Agent(OllamaChatClient(model="gemma3:4b"))

middleware = (
    PIIMiddleware
        .profile("standard")
        .llm_agent(validator)
        .build()
)
```

> The validator receives the message and the list of detected entities, and responds with `allow` or `block`. This significantly reduces false positives on ambiguous inputs like dates or reference numbers.

---

## ⚙️ How It Works

```
1. Intercept   →  middleware captures the last user message
2. Detect      →  Recognizers Text extracts entity types
3. Filter      →  entities not in allow_list are candidates
4. Match       →  candidates matched against block_list
5. Validate    →  (optional) LLM agent makes final decision
6. Block / Pass →  blocked messages short-circuit the pipeline
                   — the primary LLM is never called
```

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to change before submitting a pull request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 📄 License

MIT
