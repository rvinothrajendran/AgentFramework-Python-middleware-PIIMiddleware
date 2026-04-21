<div align="center">

# 🔍 AzureAICommunity - Agent - File Search

File search tools (by name and content) for AI agent applications built on the **Agent Framework**.

[![PyPI Version](https://img.shields.io/pypi/v/azureaicommunity-agent-file-search)](https://pypi.org/project/azureaicommunity-agent-file-search/)
[![Python Versions](https://img.shields.io/pypi/pyversions/azureaicommunity-agent-file-search)](https://pypi.org/project/azureaicommunity-agent-file-search/)
![PyPI Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-file-search)
[![License](https://img.shields.io/pypi/l/azureaicommunity-agent-file-search)](https://pypi.org/project/azureaicommunity-agent-file-search/)
[![PyPI Status](https://img.shields.io/pypi/status/azureaicommunity-agent-file-search)](https://pypi.org/project/azureaicommunity-agent-file-search/)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

**Give your agent

[Getting Started](#-installation) · [Configuration](#️-configuration) · [Usage](#-usage) · [Contributing](#-contributing)

</div>

---

## Overview

`azureaicommunity-agent-file-search` provides two `@tool`-decorated functions that can be wired directly into any `agent-framework` agent. The agent can search for files by glob pattern or scan file contents for a string — with sensible defaults and a fully configurable `SearchConfig` for fine-grained control.

---

## ✨ Features

| | Feature |
|---|---|
| 🔎 | **Search by name** — glob-pattern matching (`*.py`, `main*`, `report.pdf`) across a directory tree |
| 📄 | **Search by content** — full-text scan of file contents with case-sensitive or case-insensitive matching |
| ⚙️ | **Configurable** — control max results, depth, hidden files, extension filters, file size limits, and more |
| 🔌 | **Agent-ready** — tools decorated with `@tool`, drop straight into any `agent-framework` agent |
| 🌐 | **Encoding-aware** — tries multiple encodings (UTF-8, Latin-1) before skipping a file |
| 🚫 | **Binary-safe** — skips binary files automatically via null-byte detection |
| 🔁 | **Symlink-safe** — optional symlink following with loop detection |
| 📦 | **Provider-agnostic** — works with Ollama, Azure OpenAI, or any `agent-framework` compatible client |

---

## 📦 Installation

```bash
pip install azureaicommunity-agent-file-search
```

---

## 🚀 Quick Start

```python
import asyncio
from agent_framework.ollama import OllamaChatClient
from file_search_module import file_search_by_name, file_search_by_content, configure

# Optional: set global defaults
configure(max_results=50, max_depth=10, skip_hidden=True)

agent = OllamaChatClient(model="llama3.2").as_agent(
    name="FileSearchAgent",
    instructions="You are a helpful file-search assistant. Always search under C:\\MyProject.",
    tools=[file_search_by_name, file_search_by_content],
)

async def main():
    session = agent.create_session()
    response = await agent.run("Find all Python files in the project.", session=session)
    print(response.text)

asyncio.run(main())
```

---

## 🧑‍💻 Usage

### Search by file name

```python
from file_search_module import file_search_by_name

# All Python files
results = file_search_by_name("*.py", path="C:\\MyProject")

# Files starting with "main"
results = file_search_by_name("main*", path="C:\\MyProject")

# Case-sensitive match
results = file_search_by_name("README.md", path="C:\\MyProject", case_sensitive=True)

# Only .py and .txt files
results = file_search_by_name("*", path="C:\\MyProject", file_types=[".py", ".txt"])
```

### Search by file content

```python
from file_search_module import file_search_by_content

# Case-sensitive (default)
results = file_search_by_content("middleware", path="C:\\MyProject")

# Case-insensitive
results = file_search_by_content("TODO", path="C:\\MyProject", case_sensitive=False)

# Restrict to Python files only
results = file_search_by_content("async def", path="C:\\MyProject", file_types=[".py"])
```

### Per-call config override

```python
from file_search_module import file_search_by_name, SearchConfig

custom = SearchConfig(max_results=5, include_extensions=[".py"], skip_hidden=True)
results = file_search_by_name("*.py", path="C:\\MyProject", config=custom)
```

---

## ⚙️ Configuration

### `configure()` — set global defaults

```python
from file_search_module import configure

configure(
    max_results=100,
    max_depth=5,
    skip_hidden=True,
    exclude_extensions=[".pyc", ".pyo", ".exe", ".dll"],
    encodings=["utf-8", "latin-1"],
)
```

### `SearchConfig` fields

| Parameter | Type | Default | Description |
|---|---|---|---|
| `max_results` | `int` | `200` | Maximum number of paths returned per search call |
| `max_depth` | `int` | `20` | Maximum directory recursion depth |
| `max_file_size_bytes` | `int` | `10485760` (10 MB) | Files larger than this are skipped during content search |
| `binary_check_bytes` | `int` | `8192` | Bytes read to detect binary files (null-byte probe) |
| `follow_symlinks` | `bool` | `False` | Whether to follow symbolic links |
| `skip_hidden` | `bool` | `False` | Skip files and folders starting with `.` |
| `include_extensions` | `list[str] \| None` | `None` | Whitelist of extensions — `None` means all |
| `exclude_extensions` | `list[str] \| None` | `None` | Blacklist of extensions |
| `encodings` | `list[str]` | `["utf-8", "latin-1"]` | Encoding fallback chain for content search |

---

## ⚙️ How It Works

### `file_search_by_name`

```
1. Validate query and path inputs
2. Auto-normalize bare extensions: 'py' → '*.py', '.py' → '*.py'
3. Walk directory tree (respecting max_depth, skip_hidden, follow_symlinks)
4. For each file: check extension filters, apply glob match
5. Return relative paths, capped at max_results
```

### `file_search_by_content`

```
1. Validate query and path inputs
2. Walk directory tree (same depth/hidden/symlink rules)
3. For each file: skip if > max_file_size_bytes, skip if binary
4. Try each encoding in the fallback chain until the file is readable
5. Search for the query string (case-sensitive or insensitive)
6. Return relative paths of matching files, capped at max_results
```

---

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to change before submitting a pull request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 👤 Author

Built and maintained by **Vinoth Rajendran**.

- 🐙 GitHub: [github.com/rvinothrajendran](https://github.com/rvinothrajendran) — _follow for more projects!_
- 📺 YouTube: [youtube.com/@VinothRajendran](https://www.youtube.com/@VinothRajendran) — _subscribe for tutorials and demos!_
- 💼 LinkedIn: [linkedin.com/in/rvinothrajendran](https://www.linkedin.com/in/rvinothrajendran/) — _let's connect!_

---

## 📄 License

MIT — see [LICENSE](LICENSE) for details.
