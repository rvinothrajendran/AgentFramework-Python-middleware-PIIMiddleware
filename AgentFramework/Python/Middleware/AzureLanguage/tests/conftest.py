import pytest


class MockContent:
    def __init__(self, text):
        self.text = text


class MockMessage:
    def __init__(self, text):
        self.contents = [MockContent(text)]
        self.additional_properties = {}

    @property
    def text(self):
        return self.contents[0].text if self.contents else None


class MockResponse:
    def __init__(self, messages):
        self.messages = messages


class MockContext:
    def __init__(self, messages, result=None):
        self.messages = messages
        self.result = result


class MockAgent:
    def __init__(self, response="translated by llm"):
        self.response = response

    async def run(self, prompt):
        return self.response


@pytest.fixture
def mock_agent():
    return MockAgent()


@pytest.fixture
def mock_agent_detect():
    """Agent that returns a language code on detect calls and translated text on translate calls."""
    class SmartMockAgent:
        async def run(self, prompt):
            if "Identify the language" in prompt:
                return "fr"
            return "translated by llm"

    return SmartMockAgent()