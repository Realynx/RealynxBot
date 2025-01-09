using OpenAI.Chat;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class GptQueryGenerator : ILmQueryGenerator {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;
        private ChatClient _chatClientGpt;

        public GptQueryGenerator(ILogger logger, OpenAiConfig openAiConfig) {
            _logger = logger;
            _openAiConfig = openAiConfig;
            _chatClientGpt = new(_openAiConfig.InterpreterModelId, _openAiConfig.ApiKey);
        }

        private static List<ChatMessage> LanguageModelContext(string prompt) {
            return new List<ChatMessage> {
                new SystemChatMessage($"""
                    You are a chat bot that has google search capabilities. The current date is {DateTime.Now:G}. The following rules apply:
                    1. **Output raw google query**: You must output only the query to be used in google search.
                    2. **Generate google queries**: You must generate a google query that will provide you with the information to answer or solve the user's request.
                        - Focus on concise, and optimized queries for direct use in Google search.
                    3. **Advanced google search features**: You may use advanced search features in google search in order to find more relevant data for the user.
                    3. **Page Content**: You will be given the extracted text content from the first 10 pages returned from your google query.
                    """),

                new UserChatMessage(prompt)
            };
        }

        public async Task<string> CreateQuery(string prompt) {
            var queryContext = LanguageModelContext(prompt);

            var chatCompletion = await _chatClientGpt.CompleteChatAsync(queryContext, new ChatCompletionOptions() {
                Temperature = 0.03f,
                MaxOutputTokenCount = 50
            });

            return chatCompletion.Value.Content.First().Text;
        }
    }
}
