using Microsoft.Extensions.Configuration;

namespace RealynxBot.Models.Config {
    public class RoleWatcherConfig {

        public WatchedMessage[] WatchedMessages { get; set; }

        public RoleWatcherConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(RoleWatcherConfig)).Bind(this);
        }
    }

    public class WatchedMessage {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public Dictionary<ulong, string> Roles { get; set; }
    }
}
