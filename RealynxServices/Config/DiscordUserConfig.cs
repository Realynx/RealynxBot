using Microsoft.Extensions.Configuration;

namespace RealynxServices.Config {
    public class DiscordUserConfig {
        public string DiscordToken { get; set; } = default!;

        public DiscordUserConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(DiscordUserConfig)).Bind(this);
        }
    }
}
