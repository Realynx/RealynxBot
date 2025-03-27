using System.ComponentModel;

using PuppeteerSharp;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

using RealynxServices.Interfaces;

namespace RealynxBot.Services.Web {
    public class HeadlessBrowserService : IHeadlessBrowserService {
        private readonly ILogger _logger;
        private readonly BrowserConfig _browserConfig;

        public HeadlessBrowserService(ILogger logger, BrowserConfig browserConfig) {
            _logger = logger;
            _browserConfig = browserConfig;
        }

        private async Task<IBrowser> SetupBrowser() {
            _logger.Debug("Configuring browser...");

            var launchOptions = new LaunchOptions {
                Headless = true,
                Browser = SupportedBrowser.Chrome,
                DefaultViewport = new ViewPortOptions() {
                    Width = _browserConfig.Width ?? 1920,
                    Height = _browserConfig.Height ?? 1080
                },
                AcceptInsecureCerts = true,
            };

            if (!string.IsNullOrWhiteSpace(_browserConfig.BrowserPath)) {
                launchOptions.ExecutablePath = _browserConfig.BrowserPath;
            }
            else {
                var browserFetcher = new BrowserFetcher();

                _logger.Debug($"Downloading browser [{browserFetcher.Platform}]...");
                await browserFetcher.DownloadAsync();
            }

            var browser = await Puppeteer.LaunchAsync(launchOptions);
            _logger.Debug("Browser ready!");
            return browser;
        }

        public async Task<byte[]> ScreenshotWebsite(string webAddress, bool fullLength = false) {
            webAddress = EnsureValidWebAddress(webAddress).ToString();

            _logger.Info($"Screenshotting website: {webAddress}");
            await using var browser = await SetupBrowser();

            await using var page = await browser.NewPageAsync();
            page.Error += Page_Error;
            page.PageError += Page_PageError;

            page.Load += Page_Load;
            page.DOMContentLoaded += Page_DOMContentLoaded;

            page.Console += Page_Console;

            await page.GoToAsync(webAddress);

            return await page.ScreenshotDataAsync(new ScreenshotOptions() {
                Type = ScreenshotType.Png,
                FullPage = fullLength
            });
        }

        private void Page_Console(object? sender, ConsoleEventArgs e) {
            if (e.Message.Type == ConsoleType.Error) {
                //throw new JsException(e.Message.Text);
            }

            _logger.Debug($"Js Console: '{e.Message.Text}'");
        }

        private void Page_PageError(object? sender, PageErrorEventArgs e) {
            _logger.Error($"Page Error: {e.Message}");
        }

        private void Page_DOMContentLoaded(object? sender, EventArgs e) {
            _logger.Info("DOM Content has finished loading.");
        }

        private void Page_Load(object? sender, EventArgs e) {
            _logger.Info("Page and resources has finished loading.");
        }

        private void Page_Error(object? sender, PuppeteerSharp.ErrorEventArgs e) {
            _logger.Error($"Error: {e.Error}");
        }

        private Uri EnsureValidWebAddress(string webAddress) {
            if (Uri.TryCreate(webAddress, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
                return uriResult;
            }

            if (Uri.TryCreate("https://" + webAddress, UriKind.Absolute, out uriResult)) {
                return uriResult;
            }

            throw new ArgumentException("Invalid web address. Could not create a valid URI.", nameof(webAddress));
        }

        [Description("Executes the provided JavaScript code within a secure web-browser sandbox and returns the output as an array of strings. Use this to dynamically evaluate and execute JavaScript.")]
        public async Task<string[]> ExecuteJs(string js) {
            _logger.Info($"Executeing JavaScrpipt\n{js}");

            await using var browser = await SetupBrowser();
            await using var page = await browser.NewPageAsync();

            var consoleOutput = new List<string>();

            page.Console += (sender, eventArgs) => consoleOutput.Add(eventArgs.Message.Text);
            _ = await page.EvaluateExpressionAsync(js);

            return [.. consoleOutput];
        }
    }
}
