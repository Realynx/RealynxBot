using System.Text.Json;

using Microsoft.Extensions.AI;

namespace RealynxBot.Extensions {
    internal static class LmInferenceExtensions {
        public static async Task<string> GetTextResponse(this IChatClient chatClient, string thoughtContext, ChatOptions chatOptions = default) {
            var chatResponse = await chatClient.GetResponseAsync(thoughtContext, chatOptions);

            return chatResponse.Messages.FirstOrDefault() is not ChatMessage firstResponseChoice ||
                firstResponseChoice.Contents.FirstOrDefault() is not TextContent firstTextResponse
                ? string.Empty
                : firstTextResponse.Text;
        }

        public static async Task<string> GetTextResponse(this IChatClient chatClient, List<ChatMessage> thoughtContext, ChatOptions chatOptions = default) {
            var chatResponse = await chatClient.GetResponseAsync(thoughtContext, chatOptions);

            return chatResponse.Messages.FirstOrDefault() is not ChatMessage firstResponseChoice ||
                firstResponseChoice.Contents.FirstOrDefault() is not TextContent firstTextResponse
                ? string.Empty
                : firstTextResponse.Text;
        }

        public static async Task<dynamic> GetJsonResponse(this IChatClient chatClient, List<ChatMessage> thoughtContext, string jsonSchemaString, Type type,
            float temperature = 0f, string? schemaName = null) {
            var jsonSchemaElement = JsonSerializer.Deserialize<JsonElement>(jsonSchemaString);

            var thoughtCompletion = await chatClient.GetResponseAsync(thoughtContext, new ChatOptions {
                Temperature = temperature,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(jsonSchemaElement, schemaName)
            });

            if (thoughtCompletion.Messages.FirstOrDefault() is not ChatMessage firstResponseChoice ||
                firstResponseChoice.Contents.FirstOrDefault() is not TextContent firstTextResponse) {
                throw new Exception("Thought failed!");
            }

            object result;
            try {
                result = JsonSerializer.Deserialize(firstTextResponse.Text, type)
                    ?? throw new Exception("Could not read the json response from the LLM!");
            }
            catch (Exception ex) {
                throw new Exception("Failed to deserialize the response.", ex);
            }

            return result;
        }
    }
}
