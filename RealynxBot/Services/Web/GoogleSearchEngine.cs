using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.CustomSearchAPI.v1.Data;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Web {
    internal class GoogleSearchEngine : IGoogleSearchEngine {
        private readonly ILogger _logger;
        private readonly GoogleApiConfig _googleApiConfig;

        public GoogleSearchEngine(ILogger logger, GoogleApiConfig googleApiConfig) {
            _logger = logger;
            _googleApiConfig = googleApiConfig;
        }


        public async Task<Result[]> SearchGoogle(string searchQuery) {
            var cfg = new Google.Apis.Services.BaseClientService.Initializer() {
                ApiKey = _googleApiConfig.CustomSearchApiKey,
            };

            var results = new List<Result>();

            var search = new CustomSearchAPIService(cfg);
            var listRequest = search.Cse.List();
            listRequest.Q = searchQuery;
            listRequest.Cx = _googleApiConfig.CustomSearchEngineId;

            var searchResult = await listRequest.ExecuteAsync();
            results.AddRange(searchResult?.Items is null ? Array.Empty<Result>() : searchResult.Items.ToArray());

            _logger.Info($"Got {results.Count} search result items.");
            return results.ToArray();
        }
    }
}