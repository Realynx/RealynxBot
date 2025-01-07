using Discord;
using Discord.Interactions;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord.Commands {
    public class OpenAiCommands : InteractionModuleBase<SocketInteractionContext> {
        private readonly IGptChatService _gptChatService;
        private readonly ILogger _logger;
        private readonly IDiscordResponseService _discordResponseService;

        public OpenAiCommands(IGptChatService gptChatService, ILogger logger, IDiscordResponseService discordResponseService) {
            _gptChatService = gptChatService;
            _logger = logger;
            _discordResponseService = discordResponseService;
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("gpt", "Replies with an AI generated response from GPT.")]
        public async Task PromptAi(string prompt) {
            await DeferAsync();

            try {
                var gptResponse = await _gptChatService.GenerateResponse(prompt, Context.User.Username);
                foreach (var chunk in _discordResponseService.ChunkMessageToLines(gptResponse)) {
                    await FollowupAsync(chunk);
                }
            }
            catch (Exception e) {
                await FollowupAsync("There was an error.");
                _logger.Error($"Error: {e}");
            }
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("google", "Uses GPT AI to query google and provide comprehensive results.")]
        public async Task SearchGoogle(string query) {
            await DeferAsync();

            try {
                var gptResponse = await _gptChatService.SearchGoogle(query);
                foreach (var chunk in _discordResponseService.ChunkMessageToLines(gptResponse)) {
                    await FollowupAsync(chunk);
                }
            }
            catch (Exception e) {
                await FollowupAsync("There was an error.");
                _logger.Error($"Error: {e}");
            }
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("website", "Visit a host and summarize the page or response. Optionally you can add a prompt to the website.")]
        public async Task SummarizeWebsite(string websiteUrl, string question = "") {
            await DeferAsync();

            try {
                var gptResponse = await _gptChatService.SummerizeWebsite(websiteUrl, question);
                foreach (var chunk in _discordResponseService.ChunkMessageToLines(gptResponse)) {
                    await FollowupAsync(chunk);
                }
            }
            catch (Exception e) {
                await FollowupAsync("There was an error.");
                _logger.Error($"Error: {e}");
            }
        }
    }
}
