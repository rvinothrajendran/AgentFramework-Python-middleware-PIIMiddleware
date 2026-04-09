
from .security_profiles import SECURITY_PROFILES
from .pii_middleware import PIIDetectionMiddleware


class PIIMiddleware:

    def __init__(self):

        self._allow = []
        self._block = []
        self._enable_pii = False
        self._llm_agent = None

    @classmethod
    def profile(cls, profile):

        obj = cls()

        if isinstance(profile, str):
            profile_data = SECURITY_PROFILES.get(profile)
        else:
            profile_data = profile

        obj._allow = profile_data.get("allow", [])
        obj._block = profile_data.get("block", [])
        obj._enable_pii = True

        return obj

    def llm_agent(self, agent):

        self._llm_agent = agent
        return self

    def allow_entities(self, entities):

        self._allow = entities
        return self

    def block_entities(self, entities):

        self._block = entities
        return self

    def build(self):

        middleware_pipeline = []

        if self._enable_pii:
            middleware_pipeline.append(
                PIIDetectionMiddleware(
                    allow_list=self._allow,
                    block_list=self._block,
                    llm_agent=self._llm_agent
                )
            )

        return middleware_pipeline
