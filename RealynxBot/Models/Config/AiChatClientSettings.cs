using Microsoft.Extensions.Configuration;

namespace RealynxBot.Models.Config {
    internal class AiChatClientSettings {
        public AiChatClientSettings(IConfiguration configuration) {
            configuration.GetSection(nameof(AiChatClientSettings)).Bind(this);
        }

        public string HttpEndpoint { get; set; }
        public string ChatModel { get; set; }
        public string ToolModel { get; set; }
        public string ImageModel { get; set; }
        public string GrammarModel { get; set; }
        public string SpeechModel { get; set; }
    }
}
