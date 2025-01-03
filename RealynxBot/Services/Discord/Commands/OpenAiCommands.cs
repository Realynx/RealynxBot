using Discord;
using Discord.Interactions;

using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord.Commands {
    public class OpenAiCommands : InteractionModuleBase<SocketInteractionContext> {
        private readonly IGptChatService _gptChatService;
        private readonly ILogger _logger;

        public OpenAiCommands(IGptChatService gptChatService, ILogger logger) {
            _gptChatService = gptChatService;
            _logger = logger;
        }

        [SlashCommand("gpt", "Replies with AI reponse from GPT.")]
        public async Task PromptAi(string prompt) {
            await DeferAsync();

            try {
                var gptResponse = await _gptChatService.GenerateResponse(prompt, Context.User.Username);
                await FollowupAsync(gptResponse);
            }
            catch (Exception e) {
                await FollowupAsync("There was an error with OpenAI API.");
                _logger.Error($"Error: {e}");
            }
        }

        [SlashCommand("google", "Uses GPT AI to query google and provide comprehensive results.")]
        public async Task SearchGoogle(string query) {
            await DeferAsync();

            try {
                var gptResponse = await _gptChatService.SearchGoogle(query);
                var sentMessage = await FollowupAsync(gptResponse);
            }
            catch (Exception e) {
                await FollowupAsync("There was an error with OpenAI API.");
                _logger.Error($"Error: {e}");
            }
        }

        [UserCommand("test")]
        public async Task UserTest(IUser a) {
            Console.WriteLine("thing");
        }
    }
}
