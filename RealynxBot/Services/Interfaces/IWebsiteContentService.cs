
namespace RealynxBot.Services.Web {
    internal interface IWebsiteContentService {
        Task<string> GrabSiteContent(string url, int charLimit = 0);
    }
}