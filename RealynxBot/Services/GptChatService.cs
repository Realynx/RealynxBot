using System.Text;

using Google.Apis.CustomSearchAPI.v1.Data;

using OpenAI.Chat;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

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

            _chatHistory.Add(new SystemChatMessage("""
                Chat messages will be prefixed with the user's discord name; in example 'Poofyfox: [message prompt]'.
                On a second note DO NOT PING EVERYONE, or any other group. You can ping indavidual users though.
                """));
            _chatHistory.AddRange(openAiConfig.ChatBotSystemMessages.Select(i => new SystemChatMessage(i)));
        }

        public async Task<string> GenerateResponse(string prompt, string username) {
            _logger.Debug($"Prompting Gpt: '{prompt}'");

            PruneContextHistory();
            _chatHistory.Add(new UserChatMessage($"{username}: {prompt}"));

            var chatCompletion = await _chatClientGpt.CompleteChatAsync(_chatHistory);
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
                new SystemChatMessage($"""
                    Here are the google engine results for you to analyze. The search query was: '{effectiveQuery}'
                    Your objective is to summarize all the data in the search results to provide an answer and or more context to the user's query.
                    Make sure to use these links in your summarization to so the user may navigate to a result if you find it a high quality result.
                    Your messages should be short and concise. Keep in mind the results are in order of relevance.
                    The results may be empty, if they are, inform the user of the lack of information on Google for that search term.
                    The remaining system directives are information for your personality.

                    """),
            };
            queryContext.AddRange(_openAiConfig.ChatBotSystemMessages.Select(i => new SystemChatMessage(i)));

            var siteResetEvents = results
                .Select(GetDomainNameWithTld)
                .Distinct()
                .ToDictionary(x => x, _ => new ManualResetEventSlim(true));

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
                _logger.Debug($"{result.Link}\n{websiteTextualContent}");
                stringBuilder.AppendLine($"Body Text Content: {websiteTextualContent}");
                siteResetEvent.Set();

                resultContexts[tuple.Index] = new SystemChatMessage(stringBuilder.ToString());
            });

            queryContext.AddRange(resultContexts);
            _logger.Info("Finished content extraction");

            var chatCompletion = await _chatClientInterpreter.CompleteChatAsync(queryContext);
            var chatMessage = chatCompletion.Value.Content.First().Text;

            return chatMessage;
        }

        private static string GetDomainNameWithTld(Result result) {
            var split = result.DisplayLink.Split('.');
            return $"{split[^2]}.{split[^1]}";
        }

        private async Task<string> ImproveQuery(string query) {
            var queryContext = new List<ChatMessage> {
                new SystemChatMessage("""
                    The user is attempting to craft a Google search query.
                    sometimes the user could be asking you to create a Google query from a prompt, form a query to find results that best answer their prompt in these cases.
                    Your response will be used as the search query parameter in google, the raw value from your response will be used only use quotes or other features if you are intending to use advanced search features.
                    You have access to all the advanced search features in Google. But DON'T use quotes for exact each terms unless that is what the user wants explicitly. Most queries should be un-quoted search terms.

                    """),

                new UserChatMessage(query)
            };

            var chatCompletion = await _chatClientInterpreter.CompleteChatAsync(queryContext, new ChatCompletionOptions() {
                Temperature = 0,
                MaxOutputTokenCount = 50
            });

            return chatCompletion.Value.Content.First().Text;
        }

        public async Task<string> SummerizeWebsite(string websiteUrl, string prompt) {
            _logger.Info($"Grabbing website content from: {websiteUrl}");

            var queryContext = new List<ChatMessage> {
                new SystemChatMessage($"""
                    Summarize all the data in the website data, we have extracted the text from the html content.
                    Here is the user's specific question: '{prompt}', if this is empty then just summerize the website.
                    The remaining system directives are information for your personality module.
                    
                    """),
            };
            queryContext.AddRange(_openAiConfig.ChatBotSystemMessages.Select(i => new SystemChatMessage(i)));

            var websiteTextualContent = await _websiteContentService.GrabSiteContent(websiteUrl, 10000);
            queryContext.Add(new SystemChatMessage(websiteTextualContent));

            var clientResult = await _chatClientInterpreter.CompleteChatAsync(queryContext);
            var chatMessage = clientResult.Value.Content.FirstOrDefault()?.Text ?? "GPT refused to complete the chat";

            return chatMessage;
        }

        public async Task<string> GenerateJs(string prompt) {
            _logger.Info($"Generating jave script code: {prompt}");
            var queryContext = new List<ChatMessage> {
                new SystemChatMessage("""
                You are a coding assistant. Your task is to generate pure, executable JavaScript code based on the user's prompt. The following rules apply:
                1. **Output Raw JavaScript Code Only**: The output must be raw JavaScript code without any additional formatting, comments, explanations, or markdown syntax.
                2. **Execution Environment**: The code will be executed inside a sandboxed headless browser (compatible with Chrome and Firefox).
                3. **Output Handling**: 
                    - If the output is an object or array, use `JSON.stringify` with `null` as the replacer and a space value of `2` for indentation.
                    - All outputs must be directed to the console (e.g., `console.log`).
                4. **No Markdown**: Do not include any Markdown characters or text (e.g., triple backticks or language identifiers).
                5. **No Unnecessary Dependencies**: Use only JavaScript and browser-native functionality. Do not include any libraries or external dependencies.
                6. **API Usage Rules**:
                    - Only use APIs that require **no API keys or authentication** unless the user explicitly provides an API key or authentication token in their prompt.
                    - If the user provides an API key or token in the prompt, you may include it in the API call but do not modify it in any way.
                    - Prefer APIs that are publicly accessible and require no setup by the user to function.
                7. **Error Handling**:
                    - If a request cannot be fulfilled, respond in JavaScript with an appropriate error message logged to the console. 
                8. **Execution Assurance**: The output must be valid and executable JavaScript code

                """),
                new UserChatMessage(prompt)
            };

            var clientResult = await _chatClientGpt.CompleteChatAsync(queryContext, new ChatCompletionOptions() {
                Temperature = .05f,
            });

            var chatMessage = clientResult.Value.Content.FirstOrDefault()?.Text ?? "GPT refused to complete the chat";
            return chatMessage;
        }
    }
}
