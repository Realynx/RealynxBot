using System.ComponentModel;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.AiTools {
    internal class DiscordAiPlugins : IDiscordAiPlugins {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IHeadlessBrowserService _headlessBrowserService;
        private readonly SocketGuild _mainGuild;
        private readonly SocketTextChannel _mainChannel;

        public DiscordAiPlugins(ILogger logger, DiscordSocketClient discordSocketClient, IHeadlessBrowserService headlessBrowserService) {
            _logger = logger;
            _discordSocketClient = discordSocketClient;
            _headlessBrowserService = headlessBrowserService;
            _mainGuild = _discordSocketClient.Guilds.Single(i => i.Name == "Realynx Community");
            _mainChannel = _mainGuild.TextChannels.Single(i => i.Name == "satori");
        }

        [Description("Sends a message to the current channel to update the user on your execution status. Use this to make status updates for the user to know where you're at.")]
        public async Task MessageStatusUpdate(string status) {
            await _mainChannel.SendMessageAsync(status, allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
        }

        [Description("Creates a thread on discord, use this to create a thread for users to use as a new chat room. Any user may call this tool.")]
        public async Task CreateThread(string threadName) {
            await _mainChannel.CreateThreadAsync(threadName);
        }

        [Description("Create a server invite link for the main discord guild.")]
        public async Task<string> CreateServerInvite() {
            var invite = await _mainChannel.CreateInviteAsync();
            return invite.Url;
        }

        [Description("Screenshots a website with a headless browser and uploads it to the current discord channel.")]
        public async Task ScreenshotWebsite(string websiteUrl, bool fullsize = false) {
            var screenshotData = await _headlessBrowserService.ScreenshotWebsite(websiteUrl, fullsize);
            using var fileMemStream = new MemoryStream(screenshotData);

            var attachment = new FileAttachment(fileMemStream, "website-screenshot.png");
            await _mainChannel.SendFileAsync(attachment);
        }

        [Description("Will grab detailed profile information from a profile in discord using their discord id. This will also contain general information like join-date and avatar url, roles, and bot status.")]
        public async Task<string> GrabProfileInformation(string username) {
            var userSearch = await _mainGuild.SearchUsersAsync(username);
            if (userSearch.FirstOrDefault() is not RestGuildUser targetUser) {
                return string.Empty;
            }

            return $"""
                IsBot: {targetUser.IsBot}
                Username: {targetUser.Username}
                DisplayName: {targetUser.DisplayName}
                Nickname: {targetUser.Nickname}
                AvatarUrl: {targetUser.GetAvatarUrl()}

                CreatedDate: {targetUser.CreatedAt:F}
                JoinDate: {targetUser.JoinedAt:F}

                Current Activities: {string.Join("\n", targetUser.Activities.Select(i => $"Activity Name: {i.Name}, Activity Details: {i.Details}"))}
                """;
        }

        [Description("Will return a list of strings for each channel name in the current guild.")]
        public IList<string> ListChannels() {
            return _mainGuild.TextChannels.Select(i => i.Name).ToList();
        }
    }
}
