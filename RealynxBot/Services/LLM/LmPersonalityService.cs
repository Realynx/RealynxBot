using Microsoft.Extensions.AI;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM.Gpt {
    internal class LmPersonalityService : ILmPersonalityService {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;

        public string GetPersonalityPrompt {
            get {
                return $"""
                This is your personality rule set for guiding responses:
                {string.Join(Environment.NewLine, _openAiConfig.ChatBotSystemMessages)}
                """;
            }
        }

        public LmPersonalityService(ILogger logger, OpenAiConfig openAiConfig) {
            _logger = logger;
            _openAiConfig = openAiConfig;
        }

        public void AddPersonalityContext(List<ChatMessage> languageModelContext) {
            var configuredPersonality = $"""
                This is your personality rule set for guiding responses:
                {string.Join(Environment.NewLine, _openAiConfig.ChatBotSystemMessages)}
                """;

            languageModelContext.Add(new ChatMessage(ChatRole.System, configuredPersonality));
        }
    }
}