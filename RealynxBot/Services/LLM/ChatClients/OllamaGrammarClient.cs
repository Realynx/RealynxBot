using Microsoft.Extensions.AI;

using RealynxBot.Models.Config;

namespace RealynxBot.Services.LLM.ChatClients {
    internal class OllamaGrammarClient {
        private readonly AiChatClientSettings _aiChatClientSettings;

        public IChatClient ChatClient { get; }

        public OllamaGrammarClient(AiChatClientSettings aiChatClientSettings) {
            _aiChatClientSettings = aiChatClientSettings;

            ChatClient = new OllamaChatClient(_aiChatClientSettings.HttpEndpoint, modelId: _aiChatClientSettings.GrammarModel)
                .AsBuilder()
                .Build();
        }
    }
}
