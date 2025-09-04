using Microsoft.Extensions.AI;

using OllamaSharp;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM.ChatClients {
    internal class OllamaSpeechClient {
        private readonly ILogger _logger;
        private readonly AiChatClientSettings _aiChatClientSettings;

        public IChatClient ChatClient { get; }

        public OllamaSpeechClient(ILogger logger, AiChatClientSettings aiChatClientSettings) {
            _logger = logger;
            _aiChatClientSettings = aiChatClientSettings;

            ChatClient = new OllamaApiClient(_aiChatClientSettings.HttpEndpoint, defaultModel: _aiChatClientSettings.SpeechModel);
        }
    }
}
