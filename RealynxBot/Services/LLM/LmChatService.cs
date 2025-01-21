using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmChatService : ILmChatService {
        private readonly ILogger _logger;
        private readonly ILmPersonalityService _lmPersonalityService;

        private readonly IChatClient _chatClient;
        private readonly IGlobalChatContext _globalChatContext;

        public LmChatService(ILogger logger, ILmPersonalityService lmPersonalityService, IGlobalChatContext globalChatContext, OllamaUserChatClient ollamaUserChatClient) {
            _lmPersonalityService = lmPersonalityService;
            _chatClient = ollamaUserChatClient.ChatClient;
            _globalChatContext = globalChatContext;
            _logger = logger;
        }

        public async Task<string> GenerateResponse(string prompt, string username) {
            _globalChatContext.AddNewChat(username, $"""
                You are a chat assistant inside of discord. Your task is to chat with and help users. The following rules apply:
                1. **Chat messages**:
                    - Chat messages will be prefixed with the user's discord name in example: 'Poofyfox: [message prompt]'.
                2. **Tagging/Pinging**:
                    - DO NOT PING EVERYONE, You can ping individual users.
                3. **Clean Response**:
                    - Your response should only include the text, do not append anything other then your response text.

                {_lmPersonalityService.GetPersonalityPrompt}
                """);

            var chatResponse = await _globalChatContext.ChatAndAdd(_chatClient, username, new ChatMessage(ChatRole.User, $"{username}: {prompt}"));
            return chatResponse;
        }
    }
}
