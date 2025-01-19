using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class LmChatService : ILmChatService {
        private readonly ILogger _logger;
        private readonly ILmPersonalityService _lmPersonalityService;

        private readonly IChatClient _chatClient;
        private readonly List<ChatMessage> _chatHistory = new();

        public LmChatService(ILogger logger, ILmPersonalityService lmPersonalityService, IChatClient chatClient) {
            _lmPersonalityService = lmPersonalityService;
            _chatClient = chatClient;
            _logger = logger;

            _chatHistory.Add(new(ChatRole.System, """
                You are a chat assistant inside of discord. Your task is to chat with and help users. The following rules apply:
                1. **Chat messages**:
                    - Chat messages will be prefixed with the user's discord name in example: 'Poofyfox: [message prompt]'.
                2. **Tagging/Pinging**:
                    - DO NOT PING EVERYONE, You can ping individual users.
                3. **Clean Response**:
                    - Your response should only include the text, do not append anything other then your response text.
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

        public async Task<string> GenerateStatus() {
            var contextHistory = new List<ChatMessage>();
            contextHistory.AddRange(_chatHistory
                .Where(i => i.Role == ChatRole.User || i.Role == ChatRole.Assistant).ToArray());

            _lmPersonalityService.AddPersonalityContext(contextHistory);
            contextHistory.Add(new ChatMessage(ChatRole.System, """
                You are a chat assistant inside of discord. Your objective is to create a funny discord status given the current chat history context.

                The code that invokes this prompt is as follows:
                ```cs
                    var currentStatus = await _lmChatService.GenerateStatus();
                    await _discordSocketClient.SetGameAsync(currentStatus);
                ```

                Here are the rules to follow;
                1. **Clean Response**:
                    - Your response should only include the text to set as your current status, do not append anything other then your response text.
                    - Your response must not be directed at a user.
                    - The response is the bot's current activity status.
                2. **Concise**:
                    - Your created status will be set as the bot's current "playing" status. That users see when they view your profile.
                    - It must fit within an activity status, it cannot be too long!
                    - You must only produce 4 - 8 words.
                """));

            var chatCompletion = await _chatClient.CompleteAsync(contextHistory, new ChatOptions() {
                MaxOutputTokens = 15,
            });

            var statusMessage = chatCompletion.Message.Text ?? string.Empty;
            return statusMessage;
        }
    }
}
