using Microsoft.Extensions.DependencyInjection;

using RealynxBot.Services.AiTools;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM;
using RealynxBot.Services.LLM.ChatClients;
using RealynxBot.Services.LLM.Gpt;
using RealynxBot.Services.Web;

namespace RealynxBot.Extensions {
    public static class ServiceCollectionExtentions {
        public static IServiceCollection AddRealynxAiServices(this IServiceCollection serviceDescriptors) {
            serviceDescriptors
                .AddSingleton<OllamaUserChatClient>()
                .AddSingleton<OllamaToolClient>()
                .AddSingleton<OllamaImageClient>()

                .AddSingleton<ILmPersonalityService, LmPersonalityService>()
                .AddSingleton<ILmChatService, LmChatService>()
                .AddSingleton<ILmCodeGenerator, LmCodeGenerator>()
                .AddSingleton<ILmQueryGenerator, LmQueryGenerator>()
                .AddSingleton<ILmWebsiteAnalyzer, LmWebsiteAnalyzer>()
                .AddSingleton<ILmToolInvoker, LmToolInvoker>()
                .AddSingleton<IGlobalChatContext, GlobalChatContext>()
                .AddSingleton<ILmStatusGenerator, LmStatusGenerator>()
                .AddSingleton<ILmContexAwareness, LmContexAwareness>()
                .AddSingleton<IDiscordAiPlugins, DiscordAiPlugins>()
                .AddSingleton<ILmComputerVision, LmComputerVision>()

                .AddSingleton<IGoogleSearchEngine, GoogleSearchEngine>()
                .AddSingleton<IWebsiteContentService, WebsiteContentService>()
                .AddSingleton<IHeadlessBrowserService, HeadlessBrowserService>();


            return serviceDescriptors;
        }
    }
}
