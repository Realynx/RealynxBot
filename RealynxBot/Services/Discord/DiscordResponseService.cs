using RealynxBot.Services.Discord.Interfaces;

using RealynxServices.Interfaces;

namespace RealynxBot.Services.Discord {
    public class DiscordResponseService : IDiscordResponseService {
        private readonly int _maxCharLength = 2000;
        private readonly ILogger _logger;

        public DiscordResponseService(ILogger logger) {
            _logger = logger;
        }

        public IEnumerable<string> ChunkMessageToLines(string message) {
            for (var i = 0; i < message.Length;) {
                var chunkLen = Math.Min(_maxCharLength, message.Length - i);

                if (message.Length - i > chunkLen && message[i + chunkLen] != '\n') {
                    var newLen = message.AsSpan(i, i + chunkLen).LastIndexOf('\n');
                    if (newLen != -1) {
                        chunkLen = newLen;
                    }
                }

                // TODO: Maintain markdown formatting like code blocks and boldness
                yield return message.Substring(i, chunkLen);

                i += chunkLen;
            }
        }

        public async Task<bool> ChunkMessage(string largeMessage, Func<string, Task> followupAction) {
            var success = true;
            for (var x = 0; x < largeMessage.Length; x += _maxCharLength) {
                var chunk = largeMessage.Substring(x, Math.Min(_maxCharLength, largeMessage.Length - x));

                try {
                    await followupAction(chunk);
                }
                catch (Exception) {
                    success = false;
                    break;
                }
            }

            return success;
        }
    }
}
