using Microsoft.Extensions.Configuration;

namespace RealynxBot.Models.Config {
    internal class GoogleApiConfig {
        public string CustomSearchApiKey { get; set; }
        public string CustomSrearchEngineId { get; set; }
        public GoogleApiConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(GoogleApiConfig)).Bind(this);
        }
    }
}
