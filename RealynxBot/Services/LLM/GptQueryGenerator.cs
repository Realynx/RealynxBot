using OpenAI.Chat;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class GptQueryGenerator : ILmQueryGenerator {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;
        private readonly ChatClient _chatClientGpt;

        public GptQueryGenerator(ILogger logger, OpenAiConfig openAiConfig) {
            _logger = logger;
            _openAiConfig = openAiConfig;
            _chatClientGpt = new(_openAiConfig.InterpreterModelId, _openAiConfig.ApiKey);
        }

        private static List<ChatMessage> LanguageModelContext(string prompt) {
            return new List<ChatMessage> {
                    new SystemChatMessage($"""
                        You are a query generator designed to create effective Google search queries. The current date is {DateTime.Now:G}.
                        Follow these rules to avoid over-specification and maximize the chances of finding useful information:

                        1. **Output Rules**:
                           - Output only one concise Google search query as a single line of text.
                           - Avoid explanations, formatting, or multiple queries.

                        2. **Broad, Flexible Queries**:
                           - Avoid being overly specific in the query; ensure it is broad enough to capture a wide range of relevant results.
                           - Do not rely heavily on advanced operators (e.g., `site:`, `intitle:`) unless they significantly enhance the query's relevance.

                        3. **Ambiguity Handling**:
                           - When uncertain about the user's request, create a query that captures the general topic or concept rather than overly narrowing the scope.

                        4. **Avoid Over-Specification**:
                           - Do not include unnecessary keywords or restrictive operators that could eliminate useful results.
                           - Avoid date filters or exact phrases unless specifically required by the user's prompt.

                        5. **Fallback Strategy**:
                           - If generating a precise query risks excluding results, prioritize a broader query that can be refined in later stages.

                        6. **Examples**:
                           - User Prompt: "How to improve website performance in 2024?"
                             Query: `improve website performance 2024`
                           - User Prompt: "Best practices for Kubernetes networking"
                             Query: `Kubernetes networking best practices`

                        Your goal is to craft a balanced query that provides a starting point for discovering relevant information.
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
