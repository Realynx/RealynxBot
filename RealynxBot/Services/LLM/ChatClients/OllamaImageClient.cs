using Microsoft.Extensions.AI;

using RealynxBot.Models.Config;

namespace RealynxBot.Services.LLM.ChatClients {
    internal class OllamaImageClient {
        private readonly IChatClient _chatClient;
        private readonly AiChatClientSettings _aiChatClientSettings;

        public IChatClient ChatClient { get => _chatClient; }

        public OllamaImageClient(AiChatClientSettings aiChatClientSettings) {
            _aiChatClientSettings = aiChatClientSettings;
            _chatClient = new OllamaChatClient(_aiChatClientSettings.HttpEndpoint, modelId: "llava-llama3:8b")
                .AsBuilder()
                .Build();
        }
    }
}
