using Microsoft.Extensions.AI;

using OllamaSharp;

using RealynxBot.Models.Config;

namespace RealynxBot.Services.LLM.ChatClients {
    internal class OllamaGrammarClient {
        private readonly AiChatClientSettings _aiChatClientSettings;

        public IChatClient ChatClient { get; }

        public OllamaGrammarClient(AiChatClientSettings aiChatClientSettings) {
            _aiChatClientSettings = aiChatClientSettings;

            ChatClient = new OllamaApiClient(_aiChatClientSettings.HttpEndpoint, defaultModel: _aiChatClientSettings.GrammarModel);
        }
    }
}
