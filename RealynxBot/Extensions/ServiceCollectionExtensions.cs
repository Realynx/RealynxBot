using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using RealynxBot.Models.Config;
using RealynxBot.Services.Discord;
using RealynxBot.Services.Discord.Commands;
using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM;

namespace RealynxBot.Extensions {
    public static class ServiceCollectionExtensions {
        /// <summary>
        /// Add large language model services into host builder.
        /// </summary>
        /// <param name="serviceDescriptors"></param>
        /// <returns></returns>
        public static IServiceCollection AddLanguageModelServices(this IServiceCollection serviceDescriptors) {
            serviceDescriptors
                .AddSingleton<ILmPersonalityService, GptPersonalityService>()
                .AddSingleton<ILmChatService, GptChatService>()
                .AddSingleton<ILmCodeGenerator, GptCodeGenerator>()
                .AddSingleton<ILmQueryGenerator, GptQueryGenerator>()
                .AddSingleton<ILmWebsiteAnalyzer, GptWebsiteAnalyzer>();

            return serviceDescriptors;
        }

        /// <summary>
        /// Add local configuration models to the host builder.
        /// </summary>
        /// <param name="serviceDescriptors"></param>
        /// <returns></returns>
        public static IServiceCollection AddConfigModels(this IServiceCollection serviceDescriptors) {
            serviceDescriptors
                .AddSingleton<LoggerConfig>()
                .AddSingleton<RoleWatcherConfig>()
                .AddSingleton<OpenAiConfig>()
                .AddSingleton<BrowserConfig>()
                .AddSingleton<GoogleApiConfig>()
                .AddSingleton<DiscordConfig>();

            return serviceDescriptors;
        }

        /// <summary>
        /// Add services to make discord bot work into host builder. <paramref name="configuration"/> is used to construct the discord bot config model and login the bot for a <see cref="DiscordSocketClient"/>
        /// </summary>
        /// <param name="serviceDescriptors"></param>
        /// <param name="socketClient"></param>
        /// <returns></returns>
        public static IServiceCollection AddDiscordServices(this IServiceCollection serviceDescriptors, IConfiguration configuration) {
            ManualResetEvent _manualResetEvent = new(false);
            void SetupDiscordSingletons(out DiscordSocketClient socketClient) {
                socketClient = new DiscordSocketClient(new DiscordSocketConfig {
                    LogGatewayIntentWarnings = true,
                    GatewayIntents = GatewayIntents.All ^ GatewayIntents.GuildPresences ^ GatewayIntents.GuildScheduledEvents ^ GatewayIntents.GuildInvites,
                    LogLevel = LogSeverity.Verbose,
                    MaxWaitBetweenGuildAvailablesBeforeReady = 250,
                });
                socketClient.Ready += SocketClient_Ready;

                var discordConfig = new DiscordUserConfig(configuration);
                socketClient.LoginAsync(TokenType.Bot, discordConfig.DiscordToken).Wait();
                socketClient.StartAsync().Wait();

                Console.WriteLine("Waiting for discord gateway ready.");
                _manualResetEvent.WaitOne();

                Console.WriteLine("Discord gateway is ready...");
            }

            Task SocketClient_Ready() {
                _manualResetEvent.Set();
                return Task.CompletedTask;
            }

            SetupDiscordSingletons(out var socketClient);
            serviceDescriptors
                    .AddSingleton(socketClient)
                    .AddSingleton(new InteractionService(socketClient.Rest))
                    .AddSingleton(new CommandService())
                    .AddSingleton<IDiscordNotificationService, DiscordNotificationService>()
                    .AddSingleton<IUserRoleWatcherService, UserRoleWatcherService>()
                    .AddSingleton<ICommandHandlerService, CommandHandlerService>()
                    .AddSingleton<IDiscordResponseService, DiscordResponseService>();

            return serviceDescriptors;
        }
    }
}
