using Microsoft.Extensions.Configuration;

namespace RealynxServices.Config {
    public class BrowserConfig {
        public BrowserConfig(IConfiguration configuration) {
            configuration.GetSection(nameof(BrowserConfig)).Bind(this);
        }

        public string BrowserPath { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
