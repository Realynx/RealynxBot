using OpenAI.Chat;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

using RealynxServices.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class GptChatService : ILmChatService {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;
        private readonly ILmPersonalityService _lmPersonalityService;

        private readonly ChatClient _chatClientGpt;
        private readonly List<ChatMessage> _chatHistory = new();

        public GptChatService(ILogger logger, OpenAiConfig openAiConfig, ILmPersonalityService lmPersonalityService) {
            _openAiConfig = openAiConfig;
            _lmPersonalityService = lmPersonalityService;
            _logger = logger;
            _chatClientGpt = new(_openAiConfig.GptModelId, _openAiConfig.ApiKey);

            _chatHistory.Add(new SystemChatMessage("""
                You are a chat assistant inside of discord. Your task is to chat with and help users. The following rules apply:
                1. **Chat messages**:
                    - Chat messages will be prefixed with the user's discord name in example: 'Poofyfox: [message prompt]'.
                2. **Tagging/Pinging**:
                    - DO NOT PING EVERYONE, You can ping individual users.
                """));

            _lmPersonalityService.AddPersonalityContext(_chatHistory);
        }

        private void PruneContextHistory() {
            var maxContext = 12;
            if (_chatHistory.Count > maxContext) {
                var removeCount = _chatHistory.Count - maxContext;
                _logger.Debug($"Cleaning up context, removing {removeCount} oldest");
                _chatHistory.RemoveRange(_chatHistory.Count(i => i is SystemChatMessage), removeCount);
            }
        }

        public async Task<string> GenerateResponse(string prompt, string username) {
            _logger.Debug($"Prompting Gpt: '{prompt}'");

            PruneContextHistory();
            _chatHistory.Add(new UserChatMessage($"{username}: {prompt}"));

            var chatCompletion = await _chatClientGpt.CompleteChatAsync(_chatHistory);
            var chatMessage = chatCompletion.Value.Content.First().Text;

            _chatHistory.Add(new AssistantChatMessage(chatMessage));

            return chatMessage;
        }
    }
}
