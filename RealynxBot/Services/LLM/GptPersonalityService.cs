using OpenAI.Chat;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class GptPersonalityService : ILmPersonalityService {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;

        public GptPersonalityService(ILogger logger, OpenAiConfig openAiConfig) {
            _logger = logger;
            _openAiConfig = openAiConfig;
        }

        public void AddPersonalityContext(List<ChatMessage> languageModelContext) {
            var configuredPersonality = $"""
                This is your personality rule set for guiding responses:
                {string.Join(Environment.NewLine, _openAiConfig.ChatBotSystemMessages)}
                """;

            languageModelContext.Add(new SystemChatMessage(configuredPersonality));
        }
    }
}