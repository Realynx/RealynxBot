using System.Text;

using Google.Apis.CustomSearchAPI.v1.Data;

using Microsoft.Extensions.AI;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM.Gpt {
    internal class LmWebsiteAnalyzer : ILmWebsiteAnalyzer {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;
        private readonly IWebsiteContentService _websiteContentService;
        private readonly ILmQueryGenerator _lmQueryGenerator;
        private readonly IGoogleSearchEngine _googleSearchEngine;
        private readonly ILmPersonalityService _lmPersonalityService;
        private readonly IChatClient _chatClient;

        public LmWebsiteAnalyzer(ILogger logger, OpenAiConfig openAiConfig, IWebsiteContentService websiteContentService,
            ILmQueryGenerator lmQueryGenerator, IGoogleSearchEngine googleSearchEngine, ILmPersonalityService lmPersonalityService, IChatClient chatClient) {
            _logger = logger;
            _openAiConfig = openAiConfig;
            _websiteContentService = websiteContentService;
            _lmQueryGenerator = lmQueryGenerator;
            _googleSearchEngine = googleSearchEngine;
            _lmPersonalityService = lmPersonalityService;
            _chatClient = chatClient;
        }

        private static string GetDomainNameWithTld(Result result) {
            var split = result.DisplayLink.Split('.');
            return $"{split[^2]}.{split[^1]}";
        }

        private List<ChatMessage> LanguageModelContext_SummarizeWebsite(string prompt) {
            var queryContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, $"""
                    You are a web crawler bot that is summarizing the content of a webpage the user has requested information about. The following rules apply:
                    1. **Summarize website content**: Your objective is to summarize all the data in the website's extracted text from the crawler.
                    2. **User questions**: If the user has a specific question about the website. If the question is empty then there was no specific questions about the site.
                        - specific question: '{prompt}'
                    """),
            };
            return queryContext;
        }

        public async Task<string> SummarizWebsite(string websiteUrl, string prompt) {
            _logger.Info($"Grabbing website content from: {websiteUrl}");
            var websiteTextualContent = await _websiteContentService.GrabSiteContent(websiteUrl, 10000);

            var lmContext = LanguageModelContext_SummarizeWebsite(prompt);
            lmContext.Add(new ChatMessage(ChatRole.System, websiteTextualContent));
            _lmPersonalityService.AddPersonalityContext(lmContext);

            var clientResult = await _chatClient.CompleteAsync(lmContext);
            var chatMessage = clientResult.Message.Text ?? "GPT refused to complete the chat";

            return chatMessage;
        }

        private List<ChatMessage> LanguageModelContext_AnalyzeSearchResults(object effectiveQuery) {
            var queryContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, $"""
                    Here are the google engine results for you to analyze. The search query was: '{effectiveQuery}'. The following rules apply:
                    1. **Summarize website content results**: Your objective is to summarize all the data in the search results to provide an answer and or more context to the user's query.
                    2. **Linking sources**: Make sure to use these links in your summarization to so the user may navigate to a result if you find it a high quality result.
                    3. **Brevity of response**: Your messages should be short and concise, but clearly explain the results.
                    4. **Order of relevance**: The results are in the order that google returned from the query.
                    5. **Exceptions and errors**: The results may be empty, this happens when the crawler could not extract the pages content for you.
                    """),
            };
            return queryContext;
        }

        public async Task<string> SearchWeb(string googlePrompt) {
            _logger.Info($"User query prompt: {googlePrompt}");

            var effectiveQuery = await _lmQueryGenerator.CreateQuery(googlePrompt);
            _logger.Info($"Querying google for: {effectiveQuery}");

            var results = await _googleSearchEngine.SearchGoogle(effectiveQuery);
            var extractedWebContents = await ExtractWebContentParallel(results);
            _logger.Info("Finished content extraction");

            var analysisResults = await SummarizeSearchResults(effectiveQuery, extractedWebContents);
            return analysisResults;
        }

        private async Task<string> SummarizeSearchResults(string effectiveQuery, string[] extractedWebContents) {
            var lmContext = LanguageModelContext_AnalyzeSearchResults(effectiveQuery);
            lmContext.Add(new ChatMessage(ChatRole.System, string.Join(Environment.NewLine, extractedWebContents)));
            _lmPersonalityService.AddPersonalityContext(lmContext);

            var chatCompletion = await _chatClient.CompleteAsync(lmContext);
            var chatMessage = chatCompletion.Message.Text;
            return chatMessage;
        }

        private async Task<string[]> ExtractWebContentParallel(Result[] results) {
            var siteResetEvents = results
                .Select(GetDomainNameWithTld)
                .Distinct()
                .ToDictionary(x => x, _ => new ManualResetEventSlim(true));

            var resultContexts = new string[results.Length];
            await Parallel.ForEachAsync(results.Index(), async (tuple, cancellationToken) => {
                var result = tuple.Item;
                var siteResetEvent = siteResetEvents[GetDomainNameWithTld(result)];

                siteResetEvent.Wait(cancellationToken);
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Title: {result.Title}");
                stringBuilder.AppendLine($"Link: {result.Link}");
                stringBuilder.AppendLine($"Description: {result.Snippet}");

                var websiteTextualContent = await _websiteContentService.GrabSiteContent(result.Link, 3500);
                _logger.Debug($"{result.Link}\n{websiteTextualContent}");
                stringBuilder.AppendLine($"Body Text Content: {websiteTextualContent}");
                siteResetEvent.Set();

                resultContexts[tuple.Index] = stringBuilder.ToString();
            });
            return resultContexts;
        }
    }
}
