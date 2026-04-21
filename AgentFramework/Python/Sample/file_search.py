"""
file_search.py — Demonstrates file_search_module with an Ollama-backed agent.

The agent has two tools wired in:
  • file_search_by_name    — search files by name pattern
  • file_search_by_content — full-text search inside files

Run:
    cd Sample
    python file_search.py
"""

import asyncio
import os

from agent_framework.ollama import OllamaChatClient

from file_search_module import SearchConfig, configure, get_config
from file_search_module import file_search_by_name, file_search_by_content

# ---------------------------------------------------------------------------
# 1. Global configuration — applies to every tool call unless overridden
# ---------------------------------------------------------------------------
configure(
    max_results=50,           # return at most 50 paths
    max_depth=10,             # don't recurse deeper than 10 levels
    skip_hidden=True,         # ignore dot-files / dot-folders
    exclude_extensions=[".pyc", ".pyo", ".exe", ".dll", ".bin"],
    encodings=["utf-8", "latin-1"],
)

print("Active config:", get_config())
print()

# ---------------------------------------------------------------------------
# 2. Per-call config override example (does NOT change the global default)
# ---------------------------------------------------------------------------
custom_cfg = SearchConfig(
    max_results=5,
    include_extensions=[".py"],   # only Python files
    skip_hidden=True,
)

# ---------------------------------------------------------------------------
# 3. Multi-turn agent
# ---------------------------------------------------------------------------

SEARCH_ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

INSTRUCTIONS = f"""You are a helpful file-search assistant.
You have two tools available:
  - file_search_by_name(query, path, case_sensitive): find files by glob name pattern
  - file_search_by_content(query, path, case_sensitive): find files containing a string

Always pass path="{SEARCH_ROOT}" unless the user specifies a different directory.
When presenting results, list each file on its own line.
If no files are found, say so clearly.
"""


class FileSearchAgent:
    def __init__(self, model: str = "llama3.2"):
        self.agent = OllamaChatClient(model=model).as_agent(
            name="FileSearchAgent",
            instructions=INSTRUCTIONS,
            tools=[file_search_by_name, file_search_by_content],
        )
        self.session = self.agent.create_session()

    async def ask(self, message: str) -> str:
        response = await self.agent.run(message, session=self.session)
        return response.text


async def main():
    agent = FileSearchAgent(model="llama3.2")

    scenarios = [
        # Name search — glob pattern
        "Find all Python files (*.py) in the project.",

        # Content search
        "Find files that contain the text 'middleware'.",

        # Follow-up — exercises session memory
        "Of those results, which ones also have 'async def' in their content?",

        # Edge case — no results expected
        "Search for files named '*.rs' (Rust source files).",

        # Case-insensitive content search
        "Search for files containing 'TODO' (case-insensitive).",
    ]

    for i, prompt in enumerate(scenarios, 1):
        print(f"{'='*60}")
        print(f"[Turn {i}] {prompt}")
        print("-" * 60)
        answer = await agent.ask(prompt)
        print(answer)
        print()

    # ---------------------------------------------------------------------------
    # 4. Direct tool call examples (no LLM) — useful for unit testing
    # ---------------------------------------------------------------------------
    print("=" * 60)
    print("[Direct tool calls — no LLM]")
    print("-" * 60)

    # 4a. Name search with global config
    py_files = file_search_by_name("*.py", path=SEARCH_ROOT)
    print(f"file_search_by_name('*.py') → {len(py_files)} results")
    for p in py_files[:5]:
        print(f"  {p}")
    if len(py_files) > 5:
        print(f"  ... and {len(py_files) - 5} more")
    print()

    # 4b. Name search with per-call config override
    limited = file_search_by_name("*.py", path=SEARCH_ROOT, config=custom_cfg)
    print(f"file_search_by_name('*.py', config=custom_cfg [max=5]) → {len(limited)} results")
    for p in limited:
        print(f"  {p}")
    print()

    # 4c. Content search — case-sensitive (default)
    hits = file_search_by_content("FunctionMiddleware", path=SEARCH_ROOT)
    print(f"file_search_by_content('FunctionMiddleware') → {len(hits)} results")
    for p in hits[:5]:
        print(f"  {p}")
    print()

    # 4d. Content search — case-insensitive
    hits_ci = file_search_by_content("todo", path=SEARCH_ROOT, case_sensitive=False)
    print(f"file_search_by_content('todo', case_sensitive=False) → {len(hits_ci)} results")
    for p in hits_ci[:5]:
        print(f"  {p}")
    print()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
