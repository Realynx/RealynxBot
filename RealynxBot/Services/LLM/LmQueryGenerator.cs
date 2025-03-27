using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

using RealynxServices.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class LmQueryGenerator : ILmQueryGenerator {
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;

        public LmQueryGenerator(ILogger logger, OllamaUserChatClient ollamaUserChatClient) {
            _logger = logger;
            _chatClient = ollamaUserChatClient.ChatClient;
        }

        private static List<ChatMessage> LanguageModelContext(string prompt) {
            return new List<ChatMessage> {
                    new ChatMessage(ChatRole.System, $"""
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

                new ChatMessage(ChatRole.User, prompt)
            };
        }

        public async Task<string> CreateQuery(string prompt) {
            var queryContext = LanguageModelContext(prompt);

            var chatCompletion = await _chatClient.CompleteAsync(queryContext, new ChatOptions() {
                Temperature = 0.03f,
                MaxOutputTokens = 50
            });

            return chatCompletion.Message.Text ?? string.Empty;
        }
    }
}
