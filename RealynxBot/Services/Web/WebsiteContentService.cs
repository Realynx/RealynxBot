using System.Text;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using RealynxBot.Extensions;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Web {
    internal partial class WebsiteContentService : IWebsiteContentService {
        [GeneratedRegex(@"\w{20,}")]
        private static partial Regex MangledWordRegex { get; }

        [GeneratedRegex(@"\s{2,}")]
        private static partial Regex MultiSpaceRegex { get; }

        private const string CONTENT_FETCH_FAILURE_MESSAGE = "Failed to fetch website content!";

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public WebsiteContentService(ILogger logger, HttpClient httpClient) {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<string> GrabSiteContent(string url, int charLimit = 0) {
            _logger.Info($"Visiting '{url}'");

            HttpResponseMessage httpResponseMessage;
            try {
                httpResponseMessage = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception e) {
                _logger.Error($"Error making request: {e}");
                return CONTENT_FETCH_FAILURE_MESSAGE;
            }

            if (!httpResponseMessage.IsSuccessStatusCode) {
                _logger.Error($"Could not GET url '{url}' Status Code: {httpResponseMessage.StatusCode}");
                return CONTENT_FETCH_FAILURE_MESSAGE;
            }

            if (!httpResponseMessage.Content.Headers.TryGetValues("Content-Type", out var contentHeaders)) {
                _logger.Error($"Could not GET url '{url}' Response has no Content-Type");
                return CONTENT_FETCH_FAILURE_MESSAGE;
            }

            var contentHeader = contentHeaders.FirstOrDefault(string.Empty);
            if (!contentHeader.StartsWith("text/html") && !contentHeader.StartsWith("application/xhtml+xml") && !contentHeader.StartsWith("application/xml")) {
                _logger.Error($"Could not GET url '{url}' Content-Type: {contentHeader}");
                return CONTENT_FETCH_FAILURE_MESSAGE;
            }

            string websiteContent;
            try {
                websiteContent = await httpResponseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception e) {
                _logger.Error($"Error making request: {e}");
                return CONTENT_FETCH_FAILURE_MESSAGE;
            }

            var bodyContent = ExtractWebsiteContent(websiteContent);
            bodyContent = TruncateText(charLimit, bodyContent);

            return bodyContent;
        }

        private static string TruncateText(int charLimit, string bodyContent) {
            if (charLimit > 0 && bodyContent.Length > charLimit) {
                bodyContent = MangledWordRegex.Replace(bodyContent, string.Empty);

                if (bodyContent.Length > charLimit) {
                    bodyContent = bodyContent.Remove(charLimit);
                }
            }

            return bodyContent;
        }

        private static string ExtractWebsiteContent(string websiteContent) {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(websiteContent);

            var nodes = htmlDoc.DocumentNode.SelectNodes("//article") ??
                     htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'main-content')]") ??
                     htmlDoc.DocumentNode.SelectNodes("//body//*");

            var contentBuilder = new StringBuilder();
            foreach (var node in nodes) {
                contentBuilder.Append(node.InnerText);
                contentBuilder.Append(' ');
            }

            var bodyContent = contentBuilder.Replace("\n", string.Empty).Replace("\r", string.Empty).Trim().ToString();
            bodyContent = MultiSpaceRegex.Replace(bodyContent, " ");
            return bodyContent;
        }
    }
}