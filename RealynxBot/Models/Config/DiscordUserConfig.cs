using Microsoft.Extensions.Configuration;

namespace RealynxBot.Models.Config {
    internal class DiscordUserConfig {
        public string DiscordToken { get; set; } = default!;

        public DiscordUserConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(DiscordUserConfig)).Bind(this);
        }
    }
}
