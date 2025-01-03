using System.ComponentModel.DataAnnotations;

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
            var currentResultOffset = 0;
            for (var x = 0; x < 1; x++) {
                var search = new CustomSearchAPIService(cfg);
                var listRequest = search.Cse.List();
                listRequest.Q = searchQuery;
                listRequest.Cx = _googleApiConfig.CustomSearchEngineId;
                listRequest.Start = currentResultOffset;

                var searchResult = await listRequest.ExecuteAsync();
                results.AddRange(searchResult?.Items is null ? Array.Empty<Result>() : searchResult.Items.ToArray());
                if (results.Count == 0) {
                    break;
                }
            }

            _logger.Info($"Got {results.Count} search result items.");
            return results.ToArray();
        }
    }
}