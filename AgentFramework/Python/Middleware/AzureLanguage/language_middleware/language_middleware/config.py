
class AzureTranslatorConfig:
    def __init__(self, key: str, region: str, endpoint: str = "https://api.cognitive.microsofttranslator.com"):
        self.key = key
        self.region = region
        self.endpoint = endpoint
