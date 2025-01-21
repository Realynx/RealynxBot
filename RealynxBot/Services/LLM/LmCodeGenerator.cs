using Microsoft.Extensions.AI;

using Newtonsoft.Json;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmCodeGenerator : ILmCodeGenerator {
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;

        public LmCodeGenerator(ILogger logger, OllamaUserChatClient ollamaUserChatClient) {
            _logger = logger;
            _chatClient = ollamaUserChatClient.ChatClient;
        }

        private static List<ChatMessage> GenerateLmPrompt(string prompt) {
            return new List<ChatMessage> {
                new ChatMessage(ChatRole.System, """
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
                8. **Execution Assurance**: The output must be valid and executable JavaScript code.

                Your response should always follow the structure:

                {
                  "EvalCode": "Your generated JavaScript code here"
                }
                
                """),
                new ChatMessage(ChatRole.User, prompt)
            };
        }

        public async Task<string> GenerateJs(string prompt) {
            _logger.Info($"Generating java script code: {prompt}");

            var languageModelContext = GenerateLmPrompt(prompt);
            var clientResult = await _chatClient.CompleteAsync(languageModelContext, new ChatOptions() {
                Temperature = .05f,
                ResponseFormat = ChatResponseFormat.Json
            });
            var llmMessage = clientResult.Message.Text ?? string.Empty;

            var jsonResponse = new { EvalCode = "" };
            try {
                jsonResponse = JsonConvert.DeserializeAnonymousType(llmMessage, jsonResponse);
            }
            catch (Exception) {

            }

            return jsonResponse?.EvalCode ?? "console.log('could not generate code.')";
        }
    }
}
