using System.Text;

using Google.Apis.CustomSearchAPI.v1.Data;

using OpenAI.Chat;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.Web;

namespace RealynxBot.Services {
    internal class GptChatService : IGptChatService {
        private readonly OpenAiConfig _openAiConfig;

        private readonly ILogger _logger;
        private readonly IGoogleSearchEngine _googleSearchEngine;
        private readonly IWebsiteContentService _websiteContentService;

        private ChatClient _chatClientGpt;
        private ChatClient _chatClientInterpreter;
        private List<ChatMessage> _chatHistory = new();

        public GptChatService(OpenAiConfig openAiConfig, ILogger logger, IGoogleSearchEngine googleSearchEngine, IWebsiteContentService websiteContentService) {
            _openAiConfig = openAiConfig;
            _logger = logger;
            _googleSearchEngine = googleSearchEngine;
            _websiteContentService = websiteContentService;
            _chatClientGpt = new(_openAiConfig.GptModelId, _openAiConfig.ApiKey);
            _chatClientInterpreter = new(_openAiConfig.InterpreterModelId, _openAiConfig.ApiKey);

            _chatHistory.Add(new SystemChatMessage("Chat messages will be prefixed with the user's discord name; in example 'Poofyfox: [message prompt]'. On a second note DO NOT PING EVERYONE, or any other group. You can ping indavidual users though."));
            _chatHistory.AddRange(openAiConfig.ChatBotSystemMessages.Select(i => new SystemChatMessage(i)));
        }

        public async Task<string> GenerateResponse(string prompt, string username) {
            _logger.Debug($"Prompting Gpt: '{prompt}'");

            PruneContextHistory();
            _chatHistory.Add(new UserChatMessage($"{username}: {prompt}"));

            var chatCompletion = await _chatClientGpt.CompleteChatAsync(_chatHistory, new ChatCompletionOptions() {
                MaxOutputTokenCount = 375
            });
            var chatMessage = chatCompletion.Value.Content.First().Text;

            _chatHistory.Add(new AssistantChatMessage(chatMessage));

            return chatMessage;
        }

        private void PruneContextHistory() {
            var maxContext = 12;
            if (_chatHistory.Count > maxContext) {
                var removeCount = _chatHistory.Count - maxContext;
                _logger.Debug($"Cleaning up context, removing {removeCount} oldest");
                _chatHistory.RemoveRange(_chatHistory.Count(i => i is SystemChatMessage), removeCount);
            }
        }

        public async Task<string> SearchGoogle(string googleQuery) {
            _logger.Info($"User query prompt: {googleQuery}");

            var effectiveQuery = await ImproveQuery(googleQuery);
            _logger.Info($"Querying google for: {effectiveQuery}");

            var results = await _googleSearchEngine.SearchGoogle(effectiveQuery);

            var queryContext = new List<ChatMessage> {
                new SystemChatMessage($"Here are the google engine results for you to analyze. The search query was: '{effectiveQuery}'"),
                new SystemChatMessage("Your objective is to summarize all the data in the search results to provide an answer and or more context to the user's query."),
                new SystemChatMessage("Make sure to use these links in your summarization to so the user may navigate to a result if you find it a high quality result."),
                new SystemChatMessage("Your messages should be short and concise. Keep in mind the results are in order of relevance."),
                new SystemChatMessage("The results may be empty, if they are, inform the user of the lack of information on Google for that search term."),
                new SystemChatMessage("The remaining system directives are information for your personality.")
            };
            queryContext.AddRange(_openAiConfig.ChatBotSystemMessages.Select(i => new SystemChatMessage(i)));

            var siteResetEvents = results.ToDictionary(GetDomainNameWithTld, _ => new ManualResetEventSlim(true));
            var resultContexts = new SystemChatMessage[results.Length];
            await Parallel.ForEachAsync(results.Index(), async (tuple, cancellationToken) => {
                var result = tuple.Item;
                var siteResetEvent = siteResetEvents[GetDomainNameWithTld(result)];

                siteResetEvent.Wait(cancellationToken);
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Title: {result.Title}");
                stringBuilder.AppendLine($"Link: {result.Link}");
                stringBuilder.AppendLine($"Description: {result.Snippet}");

                var websiteTextualContent = await _websiteContentService.GrabSiteContent(result.Link, 3500);
                _logger.Debug(websiteTextualContent);
                stringBuilder.AppendLine($"Body Text Content: {websiteTextualContent}");
                siteResetEvent.Set();

                resultContexts[tuple.Index] = new SystemChatMessage(stringBuilder.ToString());
            });

            queryContext.AddRange(resultContexts);

            var chatCompletion = await _chatClientInterpreter.CompleteChatAsync(queryContext, new ChatCompletionOptions() {
                MaxOutputTokenCount = 375
            });
            var chatMessage = chatCompletion.Value.Content.First().Text;

            return chatMessage;
        }

        private static string GetDomainNameWithTld(Result result) {
            var split = result.DisplayLink.Split('.');
            return $"{split[^2]}.{split[^1]}";
        }

        private async Task<string> ImproveQuery(string query) {
            var queryContext = new List<ChatMessage> {
                new SystemChatMessage("The user is attempting to craft a Google search query."),
                new SystemChatMessage("sometimes the user could be asking you to create a Google query from a prompt, form a query to find results that best answer their prompt in these cases."),
                new SystemChatMessage("Your response will be used as the search query parameter in google, the raw value from your response will be used only use quotes or other features if you are intending to use advanced search features."),
                new SystemChatMessage("You have access to all the advanced search features in Google. But DON'T use quotes for exact each terms unless that is what the user wants explicitly. Most queries should be un-quoted search terms."),
                new UserChatMessage(query)
            };

            var chatCompletion = await _chatClientInterpreter.CompleteChatAsync(queryContext, new ChatCompletionOptions() {
                Temperature = 0,
                MaxOutputTokenCount = 50
            });

            return chatCompletion.Value.Content.First().Text;
        }
    }
}
