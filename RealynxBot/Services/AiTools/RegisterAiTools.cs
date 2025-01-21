using Microsoft.Extensions.Hosting;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM;

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
            _lmToolInvoker.AddPlugins(_websiteContentService.GetType(), _websiteContentService);
            _lmToolInvoker.AddPlugins(_lmWebsiteAnalyzer.GetType(), _lmWebsiteAnalyzer);
            _lmToolInvoker.AddPlugins(_headlessBrowserService.GetType(), _headlessBrowserService);
            _lmToolInvoker.AddPlugins(_discordAiPlugins.GetType(), _discordAiPlugins);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
