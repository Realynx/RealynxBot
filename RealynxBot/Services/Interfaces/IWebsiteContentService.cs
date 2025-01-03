
namespace RealynxBot.Services.Interfaces {
    internal interface IWebsiteContentService {
        Task<string> GrabSiteContent(string url, int charLimit = 0);
    }
}