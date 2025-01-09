using Discord;
using Discord.Interactions;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord.Commands {
    public class BasicWebCommands : InteractionModuleBase<SocketInteractionContext> {
        private readonly ILogger _logger;
        private readonly IHeadlessBrowserService _headlessBrowserService;
        private readonly IDiscordResponseService _discordResponseService;

        public BasicWebCommands(ILogger logger, IHeadlessBrowserService headlessBrowserService, IDiscordResponseService discordResponseService) {
            _logger = logger;
            _headlessBrowserService = headlessBrowserService;
            _discordResponseService = discordResponseService;
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("screenshot", "Takes a screenshot of a website and uploads it to the channel.")]
        public async Task ScreenshotWebsite(string webAddress, bool fullLength = false) {
            await DeferAsync();

            var screenshotData = await _headlessBrowserService.ScreenshotWebsite(webAddress, fullLength);
            using var stream = new MemoryStream(screenshotData);

            await FollowupWithFileAsync(stream, $"Screenshot-{webAddress}-{DateTime.Now:g}.png", "Here is your screenshot!");
        }
    }
}
