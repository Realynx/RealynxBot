using System.Reflection;

using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord.Commands {
    public class CommandHandlerService : ICommandHandlerService {

        private readonly CommandService _commandService;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;
        private readonly IUserRoleWatcherService _userRoleWatcherService;
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _discordSocketClient;

        public CommandHandlerService(DiscordSocketClient discordSocketClient, CommandService commandService, InteractionService interactionService,
            IServiceProvider services, IUserRoleWatcherService userRoleWatcherService, ILogger logger) {
            _discordSocketClient = discordSocketClient;
            _commandService = commandService;
            _interactionService = interactionService;
            _services = services;
            _userRoleWatcherService = userRoleWatcherService;
            _logger = logger;
        }

        public async Task InitializeAsync() {
            // await _userRoleWatcherService.WatchRoles();

            _discordSocketClient.Log += DiscordSocketClient_Log;
            _discordSocketClient.InteractionCreated += DiscordSocketClient_InteractionCreated;

            var executingAssembly = Assembly.GetExecutingAssembly();
            var commandModuleInfo = await _commandService.AddModulesAsync(executingAssembly, _services);
            var interactionModuleInfo = await _interactionService.AddModulesAsync(executingAssembly, _services);

            _logger.Info($"Added {commandModuleInfo.Sum(i => i.Commands.Count)} commands from {executingAssembly.GetName()}.");
            _logger.Info($"Added {interactionModuleInfo.Sum(i => i.SlashCommands.Count)} slash commands from {executingAssembly.GetName()}.");

            _logger.Info("Registering commands with discord");
            await _interactionService.RegisterCommandsGloballyAsync();
        }

        private async Task DiscordSocketClient_InteractionCreated(SocketInteraction arg) {
            var ctx = new SocketInteractionContext(_discordSocketClient, arg);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        }

        private Task DiscordSocketClient_Log(LogMessage arg) {
            switch (arg.Severity) {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    _logger.Error(UnwrapLogMessage(arg));
                    break;
                case LogSeverity.Warning:
                    _logger.Warning(UnwrapLogMessage(arg));
                    break;
                case LogSeverity.Info:
                    _logger.Info(UnwrapLogMessage(arg));
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    _logger.Debug(UnwrapLogMessage(arg));
                    break;
                default:
                    _logger.Info(UnwrapLogMessage(arg));
                    break;
            }

            return Task.CompletedTask;
        }

        private static string UnwrapLogMessage(LogMessage arg) {
            if (!string.IsNullOrWhiteSpace(arg.Message)) {
                return $"[{arg.Source}] {arg.Message}";
            }

            var exceptionString = arg.Exception?.ToString();
            if (string.IsNullOrWhiteSpace(exceptionString)) {
                exceptionString = "Unknown message.";
            }

            return $"[{arg.Source}] {exceptionString}";
        }
    }
}