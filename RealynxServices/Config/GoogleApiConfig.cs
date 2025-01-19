using Microsoft.Extensions.Configuration;

namespace RealynxServices.Config {
    public class GoogleApiConfig {
        public string CustomSearchApiKey { get; set; }
        public string CustomSearchEngineId { get; set; }
        public GoogleApiConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(GoogleApiConfig)).Bind(this);
        }
    }
}
