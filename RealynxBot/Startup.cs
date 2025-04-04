﻿using System.Reflection;

using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RealynxBot.Extensions;
using RealynxBot.Interfaces;
using RealynxBot.Models.Config;
using RealynxBot.Services;
using RealynxBot.Services.AiTools;
using RealynxBot.Services.Discord;
using RealynxBot.Services.Discord.Commands;
using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.Web;

namespace RealynxBot {
    public class Startup : IStartup {
        private readonly ManualResetEvent _manualResetEvent = new(false);

        public IConfiguration Configuration { get; set; } = default!;

        public void Configure(HostBuilderContext hostBuilderContext, IConfigurationBuilder configurationBuilder) {
            configurationBuilder
                .AddJsonFile("appsettings.json")
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .AddEnvironmentVariables();
        }

        public void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services) {
            SetupDiscordSingletons(out var socketClient);

            services
                .AddSingleton<LoggerConfig>()
                .AddSingleton<RoleWatcherConfig>()
                .AddSingleton<OpenAiConfig>()
                .AddSingleton<BrowserConfig>()
                .AddSingleton<GoogleApiConfig>()
                .AddSingleton<DiscordConfig>()
                .AddSingleton<AiChatClientSettings>();

            services
                .AddSingleton<ILogger, Logger>()

                .AddSingleton(socketClient)
                .AddSingleton(new InteractionService(socketClient.Rest))
                .AddSingleton(new CommandService())
                .AddSingleton<IDiscordNotificationService, DiscordNotificationService>()
                .AddSingleton<IUserRoleWatcherService, UserRoleWatcherService>()
                .AddSingleton<ICommandHandlerService, CommandHandlerService>()
                .AddSingleton<IDiscordResponseService, DiscordResponseService>()

                .AddRealynxAiServices()

                .AddHostedService<DiscordStartup>()
                .AddHostedService<SatoriUser>()
                .AddHostedService<RegisterAiTools>();

            services
                .AddHttpClient<IWebsiteContentService, WebsiteContentService>(client => {
                    client.DefaultRequestHeaders.UserAgent.Clear();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Realynx_DiscordBot/1.0 (Windows; Linux; https://github.com/Realynx)");
                    client.Timeout = TimeSpan.FromSeconds(4);
                });
        }

        private void SetupDiscordSingletons(out DiscordSocketClient socketClient) {
            socketClient = new DiscordSocketClient(new DiscordSocketConfig {
                LogGatewayIntentWarnings = true,
                GatewayIntents = GatewayIntents.All ^ GatewayIntents.GuildPresences ^ GatewayIntents.GuildScheduledEvents ^ GatewayIntents.GuildInvites,
                LogLevel = LogSeverity.Verbose,
                MaxWaitBetweenGuildAvailablesBeforeReady = 250,
            });

            socketClient.Ready += SocketClient_Ready;

            var discordConfig = new DiscordUserConfig(Configuration);
            socketClient.LoginAsync(TokenType.Bot, discordConfig.DiscordToken).Wait();
            socketClient.StartAsync().Wait();

            Console.WriteLine("Waiting for discord gateway ready.");
            _manualResetEvent.WaitOne();

            Console.WriteLine("Discord gateway is ready...");
        }

        private Task SocketClient_Ready() {
            _manualResetEvent.Set();
            return Task.CompletedTask;
        }
    }
}
