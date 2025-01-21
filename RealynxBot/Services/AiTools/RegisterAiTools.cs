
using System.Reflection;

using Microsoft.Extensions.Hosting;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM;
using RealynxBot.Services.LLM.Gpt;
using RealynxBot.Services.Web;

namespace RealynxBot.Services.AiTools {
    internal class RegisterAiTools : IHostedService {
        private readonly ILogger _logger;
        private readonly ILmToolInvoker _lmToolInvoker;
        private readonly IWebsiteContentService _websiteContentService;
        private readonly IHeadlessBrowserService _headlessBrowserService;
        private readonly IDiscordAiPlugins _discordAiPlugins;
        private readonly ILmWebsiteAnalyzer _lmWebsiteAnalyzer;

        public RegisterAiTools(ILogger logger, ILmToolInvoker lmToolInvoker,
            IWebsiteContentService websiteContentService, IHeadlessBrowserService headlessBrowserService,
            IDiscordAiPlugins discordAiPlugins, ILmWebsiteAnalyzer lmWebsiteAnalyzer) {
            _logger = logger;
            _lmToolInvoker = lmToolInvoker;
            _websiteContentService = websiteContentService;
            _headlessBrowserService = headlessBrowserService;
            _discordAiPlugins = discordAiPlugins;
            _lmWebsiteAnalyzer = lmWebsiteAnalyzer;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            var methodInfo = FindMethodInfo<WebsiteContentService>(nameof(_websiteContentService.GrabSiteContent));
            _lmToolInvoker.AddDIPlugin(_websiteContentService, methodInfo);

            methodInfo = FindMethodInfo<LmWebsiteAnalyzer>(nameof(_lmWebsiteAnalyzer.SearchWeb));
            _lmToolInvoker.AddDIPlugin(_lmWebsiteAnalyzer, methodInfo);

            methodInfo = FindMethodInfo<HeadlessBrowserService>(nameof(_headlessBrowserService.ExecuteJs));
            _lmToolInvoker.AddDIPlugin(_headlessBrowserService, methodInfo);

            methodInfo = FindMethodInfo<HeadlessBrowserService>(nameof(_headlessBrowserService.ScreenshotWebsite));
            _lmToolInvoker.AddDIPlugin(_headlessBrowserService, methodInfo);

            methodInfo = FindMethodInfo<DiscordAiPlugins>(nameof(_discordAiPlugins.CreateThread));
            _lmToolInvoker.AddDIPlugin(_discordAiPlugins, methodInfo);

            methodInfo = FindMethodInfo<DiscordAiPlugins>(nameof(_discordAiPlugins.ListChannels));
            _lmToolInvoker.AddDIPlugin(_discordAiPlugins, methodInfo);

            methodInfo = FindMethodInfo<DiscordAiPlugins>(nameof(_discordAiPlugins.GrabProfileInformation));
            _lmToolInvoker.AddDIPlugin(_discordAiPlugins, methodInfo);

            methodInfo = FindMethodInfo<DiscordAiPlugins>(nameof(_discordAiPlugins.CreateServerInvite));
            _lmToolInvoker.AddDIPlugin(_discordAiPlugins, methodInfo);

            methodInfo = FindMethodInfo<DiscordAiPlugins>(nameof(_discordAiPlugins.UploadFile));
            _lmToolInvoker.AddDIPlugin(_discordAiPlugins, methodInfo);

            methodInfo = FindMethodInfo<DiscordAiPlugins>(nameof(_discordAiPlugins.MessageStatusUpdate));
            _lmToolInvoker.AddDIPlugin(_discordAiPlugins, methodInfo);

            return Task.CompletedTask;
        }

        private MethodInfo FindMethodInfo<T>(string functionName) {
            return typeof(T).GetMethod(functionName)
                ?? throw new Exception("Could not find plugin");
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
