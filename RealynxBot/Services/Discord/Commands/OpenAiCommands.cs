using Discord;
using Discord.Interactions;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM;

namespace RealynxBot.Services.Discord.Commands {
    public class OpenAiCommands : InteractionModuleBase<SocketInteractionContext> {
        private readonly ILmChatService _gptChatService;
        private readonly ILogger _logger;
        private readonly IDiscordResponseService _discordResponseService;
        private readonly ILmCodeGenerator _lmCodeGenerator;
        private readonly IHeadlessBrowserService _headlessBrowserService;
        private readonly ILmWebsiteAnalyzer _lmWebsiteAnalyzer;

        public OpenAiCommands(ILmChatService gptChatService, ILogger logger, IDiscordResponseService discordResponseService,
            ILmCodeGenerator lmCodeGenerator, IHeadlessBrowserService headlessBrowserService, ILmWebsiteAnalyzer lmWebsiteAnalyzer) {
            _gptChatService = gptChatService;
            _logger = logger;
            _discordResponseService = discordResponseService;
            _lmCodeGenerator = lmCodeGenerator;
            _headlessBrowserService = headlessBrowserService;
            _lmWebsiteAnalyzer = lmWebsiteAnalyzer;
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
        [SlashCommand("function", "Tell GPT to generate a js function and execute it.")]
        public async Task GptFunction(string gptPrompt) {
            await DeferAsync();

            var gptJsCode = await _lmCodeGenerator.GenerateJs(gptPrompt);
            var consoleOutput = await _headlessBrowserService.ExecuteJs(gptJsCode);

            await _discordResponseService.ChunkMessage($@"'{gptPrompt}'
```javascript
{gptJsCode}
```", async message => await FollowupAsync(message));
            await _discordResponseService.ChunkMessage(string.Join("\n", consoleOutput), async message => await FollowupAsync(message));
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("google", "Uses GPT AI to query google and provide comprehensive results.")]
        public async Task SearchGoogle(string query) {
            await DeferAsync();

            try {
                var gptResponse = await _lmWebsiteAnalyzer.SearchWeb(query);
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
                var gptResponse = await _lmWebsiteAnalyzer.SummarizWebsite(websiteUrl, question);
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
