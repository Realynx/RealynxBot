using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmSpeechGenerator : ILmSpeechGenerator {
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;

        public LmSpeechGenerator(ILogger logger, OllamaSpeechClient ollamaSpeechClient) {
            _logger = logger;
            _chatClient = ollamaSpeechClient.ChatClient;
        }

        public async Task<byte[]> GenerateWavAudio(string speechText) {
            var thoughtContext = new List<ChatMessage>() {
                new(ChatRole.System, "You are a text-to-speech engine. Generate English audio content in response to user queries."),
                new(ChatRole.User, speechText)
            };

            var chatResponse = await _chatClient.CompleteAsync<AudioContent>(thoughtContext);
            var audioContent = chatResponse.Message.Contents
                .OfType<AudioContent>()
                .FirstOrDefault();

            if (audioContent == null) {
                throw new Exception("Failed to generate speech. Text response received.");
            }

            return audioContent.Data!.Value.ToArray();
        }
    }
}
