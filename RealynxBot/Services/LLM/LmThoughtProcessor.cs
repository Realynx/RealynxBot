using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmThoughtProcessor {
        private readonly ILogger _logger;
        private readonly IGlobalChatContext _globalChatContext;
        private readonly OllamaToolClient _ollamaToolClient;
        private readonly OllamaUserChatClient _ollamaUserChatClient;

        public LmThoughtProcessor(ILogger logger, IGlobalChatContext globalChatContext, OllamaToolClient ollamaToolClient, OllamaUserChatClient ollamaUserChatClient) {
            _logger = logger;
            _globalChatContext = globalChatContext;
            _ollamaToolClient = ollamaToolClient;
            _ollamaUserChatClient = ollamaUserChatClient;
        }

        public void Ponder(string chatContextId) {
            var chatContext = _globalChatContext[chatContextId];
        }
    }
}
