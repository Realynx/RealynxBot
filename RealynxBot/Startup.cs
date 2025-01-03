using System.Reflection;

using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Polly;
using Polly.Extensions.Http;

using RealynxBot.Config;
using RealynxBot.Discord;
using RealynxBot.Discord.Services;
using RealynxBot.Interfaces;
using RealynxBot.Models.Config;
using RealynxBot.Services;
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
                .AddSingleton<GoogleApiConfig>()
                .AddSingleton<DiscordConfig>();

            services
                .AddSingleton<ILogger, Logger>()

                .AddSingleton(socketClient)
                .AddSingleton(new InteractionService(socketClient.Rest))
                .AddSingleton(new CommandService())
                .AddSingleton<IDiscordNotificationService, DiscordNotificationService>()
                .AddSingleton<IUserRoleWatcherService, UserRoleWatcherService>()
                .AddSingleton<ICommandHandlerService, CommandHandlerService>()

                .AddSingleton<IGptChatService, GptChatService>()
                .AddSingleton<IGoogleSearchEngine, GoogleSearchEngine>()
                .AddSingleton<IWebsiteContentService, WebsiteContentService>()
                .AddHostedService<DiscordStartup>();

            services
                .AddHttpClient<WebsiteContentService>(client => {
                    client.DefaultRequestHeaders.UserAgent.Clear();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("RealLynx_DiscordBot/1.0 (Windows; Linux; https://github.com/Realynx)");
                    client.Timeout = TimeSpan.FromSeconds(2);
                })
                .AddPolicyHandler(GetRetryPolicy());
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private void SetupDiscordSingletons(out DiscordSocketClient socketClient) {
            socketClient = new DiscordSocketClient(new DiscordSocketConfig {
                LogGatewayIntentWarnings = true,
                GatewayIntents = GatewayIntents.All,
                LogLevel = LogSeverity.Verbose
            });

            socketClient.Ready += SocketClient_Ready;

            var discordConfig = new DiscordUserConfig(Configuration);
            socketClient.LoginAsync(TokenType.Bot, discordConfig.DiscordToken).Wait();
            socketClient.StartAsync().Wait();

            Console.WriteLine("Waiting for discord gateway ready.");
            _manualResetEvent.WaitOne();

            Console.WriteLine("Disord gateway is ready...");
        }

        private Task SocketClient_Ready() {
            _manualResetEvent.Set();
            return Task.CompletedTask;
        }
    }
}
