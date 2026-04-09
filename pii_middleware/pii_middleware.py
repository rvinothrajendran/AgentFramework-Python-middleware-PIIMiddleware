
from collections.abc import Awaitable, Callable

from agent_framework import ChatMiddleware, ChatContext, ChatResponse, Message

from recognizers_sequence import SequenceRecognizer
from recognizers_number import NumberRecognizer
from recognizers_date_time import DateTimeRecognizer
from recognizers_number_with_unit import NumberWithUnitRecognizer
from recognizers_text import Culture


class PIIDetectionMiddleware(ChatMiddleware):

    def __init__(self, allow_list=None, block_list=None, llm_agent=None, culture=Culture.English):

        self.allow_list = allow_list or []
        self.block_list = block_list or []
        self.llm_agent = llm_agent

        self.sequence = SequenceRecognizer(culture)
        self.number = NumberRecognizer(culture)
        self.datetime = DateTimeRecognizer(culture)
        self.unit = NumberWithUnitRecognizer(culture)

    def detect_entities(self, text):

        results = []

        results.extend(self.sequence.get_email_model().parse(text))
        results.extend(self.sequence.get_phone_number_model().parse(text))
        results.extend(self.number.get_number_model().parse(text))
        results.extend(self.datetime.get_datetime_model().parse(text))
        results.extend(self.unit.get_dimension_model().parse(text))

        return [r.type_name for r in results]

    async def llm_validate(self, message, entities):

        if not self.llm_agent:
            return True

        prompt = f"""
Determine if the following message contains sensitive personal data.

Message:
{message}

Detected entities:
{entities}

Respond with allow or block
"""

        result = await self.llm_agent.run(prompt)

        if "allow" in result.text.lower():
            return False

        return True

    async def process(self, context: ChatContext, call_next: Callable[[], Awaitable[None]]):

        message = context.messages[-1].text

        entities = self.detect_entities(message)

        filtered = [e for e in entities if e not in self.allow_list]
        blocked = [e for e in filtered if e in self.block_list]

        if blocked:

            llm_block = await self.llm_validate(message, blocked)

            if llm_block:
                block_message = f"Message blocked: sensitive information detected ({', '.join(blocked)})."
                context.result = ChatResponse(
                    messages=[Message("assistant", [block_message])],
                    finish_reason="stop",
                )
                return

        await call_next()
