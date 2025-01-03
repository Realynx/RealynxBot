using Discord;
using Discord.WebSocket;

using RealynxBot.Models.Config;
using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord {
    public class UserRoleWatcherService : IUserRoleWatcherService {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly RoleWatcherConfig _roleWatcherConfig;

        public UserRoleWatcherService(ILogger logger, DiscordSocketClient discordSocketClient, RoleWatcherConfig roleWatcherConfig) {
            _logger = logger;
            _discordSocketClient = discordSocketClient;
            _roleWatcherConfig = roleWatcherConfig;
        }

        public async Task WatchRoles() {
            _discordSocketClient.ReactionAdded += DiscordSocketClient_ReactionAdded;
            _discordSocketClient.ReactionRemoved += DiscordSocketClient_ReactionRemoved;

            await EnsureReactions();
        }

        private async Task EnsureReactions() {
            foreach (var watchedMessage in _roleWatcherConfig.WatchedMessages) {
                var guild = _discordSocketClient.GetGuild(watchedMessage.GuildId);
                var textChannel = guild.GetTextChannel(watchedMessage.ChannelId);
                var watchedMessageObject = await textChannel.GetMessageAsync(watchedMessage.MessageId);

                foreach (var role in watchedMessage.Roles) {
                    try {
                        var emoteString = role.Value;
                        var emote = ParseEmote(emoteString);
                        await watchedMessageObject.AddReactionAsync(emote);
                    }
                    catch (Exception exception) {
                        _logger.Error(exception.ToString());
                    }
                }
            }
        }

        private static IEmote ParseEmote(string emoteString) {
            return emoteString.Contains(":") ? Emote.Parse(emoteString) : new Emoji(emoteString);
        }

        private async Task DiscordSocketClient_ReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage,
            Cacheable<IMessageChannel, ulong> cacheableChannel, SocketReaction socketReaction) {
            var userMessage = cacheableMessage.HasValue ? cacheableMessage.Value : await cacheableMessage.DownloadAsync();
            if (userMessage.Author.IsBot) {
                return;
            }

            await TriggerEmoteAction(userMessage, socketReaction, async (roleId, reactingGuildUser) => {
                if (!reactingGuildUser.Roles.Any(i => i.Id == roleId)) {
                    await reactingGuildUser.AddRoleAsync(roleId);
                }
            });
        }

        private async Task DiscordSocketClient_ReactionRemoved(Cacheable<IUserMessage, ulong> cacheableMessage,
            Cacheable<IMessageChannel, ulong> cacheableChannel, SocketReaction socketReaction) {
            var userMessage = cacheableMessage.HasValue ? cacheableMessage.Value : await cacheableMessage.DownloadAsync();
            if (userMessage.Author.IsBot) {
                return;
            }

            await TriggerEmoteAction(userMessage, socketReaction, async (roleId, reactingGuildUser) => {
                if (reactingGuildUser.Roles.Any(i => i.Id == roleId)) {
                    await reactingGuildUser.RemoveRoleAsync(roleId);
                }
            });
        }

        private async Task TriggerEmoteAction(IUserMessage userMessage, SocketReaction socketReaction, Action<ulong, SocketGuildUser> onValidEmote) {
            if (_roleWatcherConfig.WatchedMessages.Any(i => i.MessageId == userMessage.Id)) {
                var roleConfig = _roleWatcherConfig.WatchedMessages.Single(i => i.MessageId == userMessage.Id);

                var reactedEmote = socketReaction.Emote;
                var roleId = FindMatchedRole(roleConfig, reactedEmote);
                if (roleId == 0) {
                    await userMessage.RemoveReactionAsync(socketReaction.Emote, socketReaction.User.Value);
                    return;
                }

                var guild = _discordSocketClient.GetGuild(roleConfig.GuildId);
                var reactingGuildUser = guild.GetUser(socketReaction.User.Value.Id);
                onValidEmote.Invoke(roleId, reactingGuildUser);
            }
        }

        private static ulong FindMatchedRole(WatchedMessage roleConfig, IEmote reactedEmote) {
            return roleConfig.Roles
                .SingleOrDefault(configEmote => {
                    if (configEmote.Value == reactedEmote.Name) {
                        return true;
                    }

                    if (reactedEmote is not Emote gildEmote || !Emote.TryParse(configEmote.Value, out _)) {
                        return false;
                    }

                    var customEmoteId = $"<:{reactedEmote.Name}:{gildEmote.Id}>";
                    return configEmote.Value == customEmoteId;
                }).Key;
        }
    }
}
