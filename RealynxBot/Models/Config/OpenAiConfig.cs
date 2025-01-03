using Microsoft.Extensions.Configuration;

namespace RealynxBot.Models.Config {
    internal class OpenAiConfig {
        public string VoiceModelId { get; set; }
        public string ModelId { get; set; }
        public string EmbeddingModelId { get; set; }
        public string ApiKey { get; set; }
        public string OrganizationId { get; set; }
        public string[] ChatBotSystemMessages { get; set; }

        public OpenAiConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(OpenAiConfig)).Bind(this);
        }
    }
}
