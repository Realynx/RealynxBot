
using Microsoft.Extensions.Hosting;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services {
    internal class DiscordStartup : IHostedService {
        private readonly ICommandHandlerService _commandHandlerService;
        private readonly IDiscordNotificationService _discordNotificationService;
        private readonly ILogger _logger;

        public DiscordStartup(ICommandHandlerService commandHandlerService, IDiscordNotificationService discordNotificationService, ILogger logger) {
            _commandHandlerService = commandHandlerService;
            _discordNotificationService = discordNotificationService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            _logger.Info("Starting discord bot services.");

            await _commandHandlerService.InitializeAsync();

            _logger.Info("Starting status");
            await _discordNotificationService.StartUpdates();

            _logger.Info("Running");
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
