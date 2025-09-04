using Microsoft.Extensions.AI;

using OllamaSharp;

using RealynxBot.Models.Config;

namespace RealynxBot.Services.LLM.ChatClients {
    internal class OllamaToolClient {
        private readonly IChatClient _chatClient;
        private readonly AiChatClientSettings _aiChatClientSettings;

        public IChatClient ChatClient { get => _chatClient; }

        public OllamaToolClient(AiChatClientSettings aiChatClientSettings) {
            _aiChatClientSettings = aiChatClientSettings;

            _chatClient = new OllamaApiClient(_aiChatClientSettings.HttpEndpoint, defaultModel: _aiChatClientSettings.ToolModel);
            _chatClient = ChatClientBuilderChatClientExtensions
                .AsBuilder(_chatClient)
                .UseFunctionInvocation()
                .Build();
        }
    }
}
