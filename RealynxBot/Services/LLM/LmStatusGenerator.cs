using System.Text.Json;

using Discord.WebSocket;

using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmStatusGenerator : ILmStatusGenerator {
        private readonly ILogger _logger;
        private readonly IGlobalChatContext _globalChatContext;
        private readonly ILmPersonalityService _lmPersonalityService;
        private readonly IChatClient _chatClient;

        public LmStatusGenerator(ILogger logger, OllamaUserChatClient ollamaUserChatClient, IGlobalChatContext globalChatContext, ILmPersonalityService lmPersonalityService) {
            _logger = logger;
            _globalChatContext = globalChatContext;
            _lmPersonalityService = lmPersonalityService;
            _chatClient = ollamaUserChatClient.ChatClient;
        }

        public async Task<string> GenerateStatus() {
            _globalChatContext.AddNewChat("discord_status", $"""
                You're a member of a Discord server. Your objective is to create a funny discord status given the current chat history context.

                Here are the rules to follow:
                1. **Clean Response**:
                   -Your response should only include the text to set as your current status, do not append anything other than your response text.
                   - The response should not be directed at a user.
                   -The response is the bot's current activity status.
                2. **Concise**:
                   -Your created status will be set as the bot's current "playing" status, visible to users when they view your profile.
                   - It must fit within an activity status, so it cannot be too long!
                   -Your response must contain 4 to 8 words.

               {_lmPersonalityService.GetPersonalityPrompt}
            """);

            var allChatContexts = _globalChatContext.GetAllContexts;
            if (allChatContexts.Length > 0) {
                var rng = new Random();
                var randomChannelContext = allChatContexts[rng.Next(0, allChatContexts.Length)];

                _globalChatContext.ImportChatContext("discord_status", randomChannelContext);
            }

            var jsonSchemaString = """
            {
                "type": "object",
                "properties": {
                "DiscordStatus": {
                    "type": "string"
                }
                },
                "required": ["DiscordStatus"]
            }
            """;
            var jsonResponse = new { DiscordStatus = "" };
            var jsonSchemaElement = JsonSerializer.Deserialize<JsonElement>(jsonSchemaString);
            var statusMessage = await _globalChatContext.InfrenceChat(_chatClient, "discord_status", jsonSchemaElement);

            try {
                jsonResponse = (dynamic)JsonSerializer.Deserialize(statusMessage, jsonResponse.GetType());
            }
            catch (Exception) {

            }

            return jsonResponse?.DiscordStatus ?? string.Empty;
        }
    }
}
