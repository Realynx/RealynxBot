using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord {
    public class DiscordResponseService : IDiscordResponseService {
        private readonly int _maxCharLength = 2000;
        private readonly ILogger _logger;

        public DiscordResponseService(ILogger logger) {
            _logger = logger;
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
