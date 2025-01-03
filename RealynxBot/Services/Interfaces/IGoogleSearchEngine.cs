
using Google.Apis.CustomSearchAPI.v1.Data;

namespace RealynxBot.Services {
    internal interface IGoogleSearchEngine {
        Task<Result[]> SearchGoogle(string searchQuery);
    }
}