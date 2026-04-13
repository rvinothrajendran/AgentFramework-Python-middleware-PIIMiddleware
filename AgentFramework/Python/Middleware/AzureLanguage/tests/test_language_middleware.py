import pytest

from language_middleware import (
    LanguageTranslationMiddleware,
    AzureTranslatorConfig,
    AzureTranslationService,
    LLMTranslationService,
    TranslationService,
)

from conftest import MockMessage, MockContext, MockResponse, MockAgent


async def noop():
    pass


# ---------------------------------------------------------------------------
# LanguageTranslationMiddleware — creation
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
async def test_create_middleware_with_azure_and_llm(mock_agent):
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(
        azure_config=config,
        llm_agent=mock_agent,
    )
    assert middleware.target_language == "en"
    assert middleware.min_confidence == 0.8
    assert isinstance(middleware.azure, AzureTranslationService)
    assert isinstance(middleware.llm, LLMTranslationService)


@pytest.mark.asyncio
async def test_create_middleware_llm_only(mock_agent):
    middleware = LanguageTranslationMiddleware.create(llm_agent=mock_agent)
    assert middleware.azure is None
    assert isinstance(middleware.llm, LLMTranslationService)


@pytest.mark.asyncio
async def test_create_middleware_no_service():
    middleware = LanguageTranslationMiddleware.create()
    assert middleware.azure is None
    assert middleware.llm is None


def test_service_property_prefers_azure(mock_agent):
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(
        azure_config=config, llm_agent=mock_agent
    )
    assert middleware._service is middleware.azure


def test_service_property_falls_back_to_llm(mock_agent):
    middleware = LanguageTranslationMiddleware.create(llm_agent=mock_agent)
    assert middleware._service is middleware.llm


def test_service_property_none_when_no_services():
    middleware = LanguageTranslationMiddleware.create()
    assert middleware._service is None


# ---------------------------------------------------------------------------
# LanguageTranslationMiddleware — process() with Azure
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
async def test_azure_translates_last_message(monkeypatch):
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(azure_config=config)

    async def fake_detect(self, text):
        return ("fr", 0.95)

    async def fake_translate(self, text, src, tgt):
        return "hello"

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", fake_translate)

    msg = MockMessage("bonjour")
    ctx = MockContext([msg])

    await middleware.process(ctx, noop)

    assert msg.contents[0].text == "hello"
    assert msg.additional_properties["language_detection"]["translated"] is True
    assert msg.additional_properties["language_detection"]["detected_language"] == "fr"
    assert msg.additional_properties["language_detection"]["original_text"] == "bonjour"


@pytest.mark.asyncio
async def test_azure_skips_translation_when_already_target_language(monkeypatch):
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(azure_config=config)

    async def fake_detect(self, text):
        return ("en", 0.99)

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)

    msg = MockMessage("hello")
    ctx = MockContext([msg])

    await middleware.process(ctx, noop)

    # text unchanged, no additional_properties written when already in target language
    assert msg.contents[0].text == "hello"
    assert "language_detection" not in msg.additional_properties


@pytest.mark.asyncio
async def test_azure_only_translates_last_message(monkeypatch):
    """History messages should not be translated — only the last one."""
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(azure_config=config)

    calls = []

    async def fake_detect(self, text):
        calls.append(text)
        return ("fr", 0.9)

    async def fake_translate(self, text, src, tgt):
        return "translated"

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", fake_translate)

    msg1 = MockMessage("how are you")
    msg2 = MockMessage("bonjour")
    ctx = MockContext([msg1, msg2])

    await middleware.process(ctx, noop)

    assert len(calls) == 1
    assert calls[0] == "bonjour"
    assert msg1.contents[0].text == "how are you"  # unchanged
    assert msg2.contents[0].text == "translated"


@pytest.mark.asyncio
async def test_azure_falls_back_to_llm_on_failure(monkeypatch, mock_agent):
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(
        azure_config=config, llm_agent=mock_agent
    )

    async def fake_detect(self, text):
        return ("fr", 0.9)

    async def failing_translate(self, text, src, tgt):
        raise RuntimeError("Azure failure")

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", failing_translate)

    msg = MockMessage("bonjour")
    ctx = MockContext([msg])

    await middleware.process(ctx, noop)

    assert msg.contents[0].text == "translated by llm"
    assert msg.additional_properties["language_detection"]["translated"] is True


# ---------------------------------------------------------------------------
# LanguageTranslationMiddleware — process() with LLM only
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
async def test_llm_detects_and_translates(monkeypatch, mock_agent):
    middleware = LanguageTranslationMiddleware.create(llm_agent=mock_agent)

    async def fake_detect(self, text):
        return ("es", 1.0)

    async def fake_translate(self, text, src, tgt):
        return "translated by llm"

    monkeypatch.setattr(LLMTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(LLMTranslationService, "translate", fake_translate)

    msg = MockMessage("hola")
    ctx = MockContext([msg])

    await middleware.process(ctx, noop)

    assert msg.contents[0].text == "translated by llm"


@pytest.mark.asyncio
async def test_no_service_passes_through():
    middleware = LanguageTranslationMiddleware.create()

    msg = MockMessage("bonjour")
    ctx = MockContext([msg])

    await middleware.process(ctx, noop)

    assert msg.contents[0].text == "bonjour"


# ---------------------------------------------------------------------------
# Back-translation (response → user's language)
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
async def test_back_translation_with_azure(monkeypatch):
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(azure_config=config)

    async def fake_detect(self, text):
        return ("es", 0.95)

    translate_calls = []

    async def fake_translate(self, text, src, tgt):
        translate_calls.append((src, tgt, text))
        if tgt == "en":
            return "What is the capital of France?"
        return "La capital de Francia es París."

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", fake_translate)

    user_msg = MockMessage("¿Cuál es la capital de Francia?")
    response_msg = MockMessage("The capital of France is Paris.")
    ctx = MockContext([user_msg], result=MockResponse([response_msg]))

    await middleware.process(ctx, noop)

    assert response_msg.contents[0].text == "La capital de Francia es París."
    src_tgt_pairs = [(s, t) for s, t, _ in translate_calls]
    assert ("es", "en") in src_tgt_pairs
    assert ("en", "es") in src_tgt_pairs


@pytest.mark.asyncio
async def test_back_translation_with_llm(monkeypatch, mock_agent):
    middleware = LanguageTranslationMiddleware.create(llm_agent=mock_agent)

    async def fake_detect(self, text):
        return ("de", 1.0)

    translate_calls = []

    async def fake_translate(self, text, src, tgt):
        translate_calls.append((src, tgt))
        return "translated"

    monkeypatch.setattr(LLMTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(LLMTranslationService, "translate", fake_translate)

    user_msg = MockMessage("Hallo")
    response_msg = MockMessage("Hello")
    ctx = MockContext([user_msg], result=MockResponse([response_msg]))

    await middleware.process(ctx, noop)

    assert ("en", "de") in translate_calls


@pytest.mark.asyncio
async def test_no_back_translation_when_same_language(monkeypatch):
    """Response must not be back-translated when user message is already in target language."""
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(azure_config=config)

    async def fake_detect(self, text):
        return ("en", 0.99)

    translate_calls = []

    async def fake_translate(self, text, src, tgt):
        translate_calls.append((src, tgt))
        return "translated"

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", fake_translate)

    user_msg = MockMessage("What is the capital of France?")
    response_msg = MockMessage("The capital of France is Paris.")
    ctx = MockContext([user_msg], result=MockResponse([response_msg]))

    await middleware.process(ctx, noop)

    assert translate_calls == []
    assert response_msg.contents[0].text == "The capital of France is Paris."


@pytest.mark.asyncio
async def test_no_back_translation_when_forward_translation_fails(monkeypatch):
    """Response must not be back-translated when forward translation returned nothing."""
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(azure_config=config)

    async def fake_detect(self, text):
        return ("es", 0.95)

    async def failing_translate(self, text, src, tgt):
        raise RuntimeError("Azure failure")

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", failing_translate)

    user_msg = MockMessage("¿Cuál es la capital de Francia?")
    response_msg = MockMessage("The capital of France is Paris.")
    ctx = MockContext([user_msg], result=MockResponse([response_msg]))

    await middleware.process(ctx, noop)

    # Forward translation failed → user_language never set → response unchanged
    assert response_msg.contents[0].text == "The capital of France is Paris."


@pytest.mark.asyncio
async def test_back_translation_azure_fails_falls_back_to_llm(monkeypatch, mock_agent):
    """Option 1: Azure back-translation failure must silently fall back to LLM."""
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(
        azure_config=config, llm_agent=mock_agent
    )

    async def fake_detect(self, text):
        return ("es", 0.95)

    async def azure_forward_ok(self, text, src, tgt):
        if tgt == "en":
            return "What is the capital of France?"
        raise RuntimeError("Azure back-translation failed")

    async def llm_translate(self, text, src, tgt):
        return "La capital de Francia es París."

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", azure_forward_ok)
    monkeypatch.setattr(LLMTranslationService, "translate", llm_translate)

    user_msg = MockMessage("¿Cuál es la capital de Francia?")
    response_msg = MockMessage("The capital of France is Paris.")
    ctx = MockContext([user_msg], result=MockResponse([response_msg]))

    await middleware.process(ctx, noop)

    assert response_msg.contents[0].text == "La capital de Francia es París."


@pytest.mark.asyncio
async def test_no_back_translation_when_user_language_is_target_azure(monkeypatch):
    """If user writes in the target language, no back-translation should happen."""
    config = AzureTranslatorConfig("key", "region")
    middleware = LanguageTranslationMiddleware.create(azure_config=config)

    async def fake_detect(self, text):
        return ("en", 0.99)

    translate_calls = []

    async def fake_translate(self, text, src, tgt):
        translate_calls.append((src, tgt))
        return "translated"

    monkeypatch.setattr(AzureTranslationService, "detect_language", fake_detect)
    monkeypatch.setattr(AzureTranslationService, "translate", fake_translate)

    user_msg = MockMessage("What is the capital of France?")
    response_msg = MockMessage("The capital of France is Paris.")
    ctx = MockContext([user_msg], result=MockResponse([response_msg]))

    await middleware.process(ctx, noop)

    # No translation should have been called (already in target language)
    assert translate_calls == []
    assert response_msg.contents[0].text == "The capital of France is Paris."


# ---------------------------------------------------------------------------
# AzureTranslationService — unit tests
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
async def test_azure_service_conforms_to_protocol():
    config = AzureTranslatorConfig("key", "region")
    service = AzureTranslationService(config)
    assert isinstance(service, TranslationService)


@pytest.mark.asyncio
async def test_azure_service_initializes_with_config():
    config = AzureTranslatorConfig("key", "region")
    service = AzureTranslationService(config)

    assert service is not None
    assert service.config is config
    assert service.config.key == "key"
    assert service.config.region == "region"


# ---------------------------------------------------------------------------
# LLMTranslationService — unit tests
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
async def test_llm_service_conforms_to_protocol(mock_agent):
    service = LLMTranslationService(mock_agent)
    assert isinstance(service, TranslationService)


@pytest.mark.asyncio
async def test_llm_detect_language_parses_iso_code():
    agent = MockAgent(response="es")
    service = LLMTranslationService(agent)
    lang, confidence = await service.detect_language("hola mundo")
    assert lang == "es"
    assert confidence == 1.0


@pytest.mark.asyncio
async def test_llm_detect_language_handles_noisy_response():
    """Regex should extract the code even if LLM returns extra words."""
    agent = MockAgent(response="The language is fr.")
    service = LLMTranslationService(agent)
    lang, _ = await service.detect_language("bonjour")
    assert lang == "fr"


@pytest.mark.asyncio
async def test_llm_detect_language_returns_unknown_on_garbage():
    agent = MockAgent(response="!!!")
    service = LLMTranslationService(agent)
    lang, _ = await service.detect_language("???")
    assert lang == "unknown"


@pytest.mark.asyncio
async def test_llm_translate_strips_result():
    agent = MockAgent(response="  Hello  ")
    service = LLMTranslationService(agent)
    result = await service.translate("Hallo", "de", "en")
    assert result == "Hello"



