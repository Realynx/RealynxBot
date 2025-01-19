using Discord.WebSocket;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord {
    public class DiscordNotificationService : IDiscordNotificationService {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly ILmChatService _lmChatService;
        private Task _statusTask = Task.CompletedTask;

        public DiscordNotificationService(ILogger logger, DiscordSocketClient discordSocketClient, ILmChatService lmChatService) {
            _logger = logger;
            _discordSocketClient = discordSocketClient;
            _lmChatService = lmChatService;
        }

        public async Task StartUpdates() {
            WatchTask();
        }

        private void WatchTask() {
            if (_statusTask.IsCompleted) {
                _statusTask = Task.Run(UpdateLoop).ContinueWith(_ => WatchTask());
            }
        }

        private async Task UpdateLoop() {
            for (; ; ) {
                var currentStatus = await _lmChatService.GenerateStatus();
                _logger.Debug($"Updating game status: {currentStatus}");

                await _discordSocketClient.SetGameAsync(currentStatus);
                Thread.Sleep(TimeSpan.FromMinutes(1.5));
            }
        }
    }
}
