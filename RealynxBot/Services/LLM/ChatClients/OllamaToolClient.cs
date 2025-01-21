using Microsoft.Extensions.AI;

using RealynxBot.Models.Config;

namespace RealynxBot.Services.LLM.ChatClients {
    internal class OllamaToolClient {
        private readonly IChatClient _chatClient;
        private readonly AiChatClientSettings _aiChatClientSettings;

        public IChatClient ChatClient { get => _chatClient; }

        public OllamaToolClient(AiChatClientSettings aiChatClientSettings) {
            _aiChatClientSettings = aiChatClientSettings;
            _chatClient = new OllamaChatClient(_aiChatClientSettings.HttpEndpoint, modelId: _aiChatClientSettings.ToolModel)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        }
    }
}
