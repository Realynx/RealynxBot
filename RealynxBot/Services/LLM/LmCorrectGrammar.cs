using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmCorrectGrammar : ILmCorrectGrammar {
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;

        public LmCorrectGrammar(ILogger logger, OllamaGrammarClient ollamaGrammarClient) {
            _logger = logger;
            _chatClient = ollamaGrammarClient.ChatClient;
        }

        public async Task<string> CorrectGrammar(string prompt) {
            var thoughtContext = new List<ChatMessage>() {
                new ChatMessage(ChatRole.User, $"""
                Correct the following grammar and explain the corrections, if there are no need for corrections simply reply with "it looks good to me";
                    ```
                    {prompt}
                    ```
                """)
            };

            var chatResponse = await _chatClient.CompleteAsync(thoughtContext);
            return chatResponse.Message.Text ?? string.Empty;
        }
    }
}
