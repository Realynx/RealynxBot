using Discord.WebSocket;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord {
    public class DiscordNotificationService : IDiscordNotificationService {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _discordSocketClient;
        private Task _statusTask = Task.CompletedTask;

        public DiscordNotificationService(ILogger logger, DiscordSocketClient discordSocketClient) {
            _logger = logger;
            _discordSocketClient = discordSocketClient;
        }

        public async Task StartUpdates() {
            //WatchTask();
        }

        private void WatchTask() {
            if (_statusTask.IsCompleted) {
                _statusTask = Task.Run(UpdateLoop).ContinueWith(_ => WatchTask());
            }
        }

        private async Task UpdateLoop() {
            for (; ; ) {
                var currentStatus = string.Empty;

                _logger.Debug($"Updating game status: {currentStatus}");

                await _discordSocketClient.SetGameAsync(currentStatus);
                Thread.Sleep(TimeSpan.FromMinutes(1.5));
            }
        }
    }
}
