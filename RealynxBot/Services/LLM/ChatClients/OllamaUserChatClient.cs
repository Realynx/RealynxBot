using Microsoft.Extensions.AI;

using OllamaSharp;

using RealynxBot.Models.Config;

namespace RealynxBot.Services.LLM.ChatClients {
    internal class OllamaUserChatClient {
        private readonly IChatClient _chatClient;
        private readonly AiChatClientSettings _aiChatClientSettings;

        public IChatClient ChatClient { get => _chatClient; }

        public OllamaUserChatClient(AiChatClientSettings aiChatClientSettings) {
            _aiChatClientSettings = aiChatClientSettings;
            _chatClient = new OllamaApiClient(_aiChatClientSettings.HttpEndpoint, defaultModel: _aiChatClientSettings.ChatModel);
        }
    }
}
