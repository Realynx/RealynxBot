using Microsoft.Extensions.Configuration;

namespace RealynxServices.Config {
    public class OpenAiConfig {
        public string VoiceModelId { get; set; }
        public string GptModelId { get; set; }
        public string InterpreterModelId { get; set; }
        public string EmbeddingModelId { get; set; }
        public string ApiKey { get; set; }
        public string OrganizationId { get; set; }
        public string[] ChatBotSystemMessages { get; set; }

        public OpenAiConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(OpenAiConfig)).Bind(this);
        }
    }
}
