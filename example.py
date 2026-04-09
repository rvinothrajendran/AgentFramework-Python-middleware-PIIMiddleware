
import asyncio

from agent_framework.ollama import OllamaChatClient
from agent_framework import Agent

from agent_pii_security import PIIMiddleware

# Basic example
middleware = (
    PIIMiddleware
        .profile("standard")
        .build()
)

async def main():
    client = OllamaChatClient(model="gemma3:4b")
    security_agent = Agent(client)

    query = "My email is r.vinoth@live.com"
    result = await security_agent.run(query, middleware=middleware)
    print(result.text)
    

asyncio.run(main())
