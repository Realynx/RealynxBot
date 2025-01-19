using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RealynxBot.Extensions;
using RealynxBot.Interfaces;
using RealynxBot.Services;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.Web;

using RealynxServices;
using RealynxServices.Config;
using RealynxServices.Extensions;
using RealynxServices.Interfaces;

namespace RealynxBot {
    public class Startup : IStartup {

        public IConfiguration Configuration { get; set; } = default!;

        public void Configure(HostBuilderContext hostBuilderContext, IConfigurationBuilder configurationBuilder) {
            configurationBuilder
                .AddJsonFile("appsettings.json")
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .AddEnvironmentVariables();
        }

        public void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services) {

            services
                .AddConfigModels()
                .AddDiscordServices(Configuration)
                .AddLanguageModelServices()
                .ConfigureSkProviders(new OpenAiConfig(Configuration))

                .AddSingleton<IGoogleSearchEngine, GoogleSearchEngine>()
                .AddSingleton<IWebsiteContentService, WebsiteContentService>()
                .AddSingleton<IHeadlessBrowserService, HeadlessBrowserService>()

                .AddSingleton<ILogger, Logger>()
                .AddHostedService<DiscordStartup>();

            services
                .AddHttpClient<IWebsiteContentService, WebsiteContentService>(client => {
                    client.DefaultRequestHeaders.UserAgent.Clear();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Realynx_DiscordBot/1.0 (Windows; Linux; https://github.com/Realynx)");
                    client.Timeout = TimeSpan.FromSeconds(4);
                });
        }
    }
}
