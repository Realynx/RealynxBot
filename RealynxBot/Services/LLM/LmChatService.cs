using Microsoft.Extensions.AI;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class LmChatService : ILmChatService {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;
        private readonly ILmPersonalityService _lmPersonalityService;

        private readonly IChatClient _chatClient;
        private readonly List<ChatMessage> _chatHistory = new();

        public LmChatService(ILogger logger, OpenAiConfig openAiConfig, ILmPersonalityService lmPersonalityService, IChatClient chatClient) {
            _openAiConfig = openAiConfig;
            _lmPersonalityService = lmPersonalityService;
            _chatClient = chatClient;
            _logger = logger;

            _chatHistory.Add(new(ChatRole.System, """
                You are a chat assistant inside of discord. Your task is to chat with and help users. The following rules apply:
                1. **Chat messages**:
                    - Chat messages will be prefixed with the user's discord name in example: 'Poofyfox: [message prompt]'.
                2. **Tagging/Pinging**:
                    - DO NOT PING EVERYONE, You can ping individual users.
                """));

            _lmPersonalityService.AddPersonalityContext(_chatHistory);
        }

        private void PruneContextHistory() {
            var maxContext = 30;
            if (_chatHistory.Count > maxContext) {
                var removeCount = _chatHistory.Count - maxContext;
                _logger.Debug($"Cleaning up context, removing {removeCount} oldest");
                _chatHistory.RemoveRange(_chatHistory.Count(i => i.Role == ChatRole.System), removeCount);
            }
        }

        public async Task<string> GenerateResponse(string prompt, string username) {
            _logger.Debug($"Prompting LLM: '{prompt}'");

            PruneContextHistory();
            _chatHistory.Add(new ChatMessage(ChatRole.User, $"{username}: {prompt}"));

            var chatCompletion = await _chatClient.CompleteAsync(_chatHistory);
            var chatMessage = chatCompletion.Message.Text ?? string.Empty;

            _chatHistory.Add(new ChatMessage(ChatRole.Assistant, chatMessage));

            return chatMessage;
        }
    }
}
