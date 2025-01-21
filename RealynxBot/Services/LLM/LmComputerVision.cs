using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmComputerVision : ILmComputerVision {
        private readonly ILogger _logger;
        IChatClient _chatClient;

        public LmComputerVision(ILogger logger, OllamaImageClient ollamaImageClient) {
            _logger = logger;
            _chatClient = ollamaImageClient.ChatClient;
        }

        public async Task<string> DescribeImage(List<ChatMessage> chatContext, byte[] imageData, string mimeType) {
            var thoughtContext = new List<ChatMessage>();

            thoughtContext.AddRange(chatContext);
            thoughtContext.Add(new ChatMessage(ChatRole.User, [new ImageContent(imageData, mimeType)]));

            var llmResponse = await _chatClient.CompleteAsync(thoughtContext);
            return llmResponse.Message.Text ?? string.Empty;
        }
    }
}
