
namespace RealynxBot.Services.LLM {
    public interface ILmWebsiteAnalyzer {
        Task<string> SearchWeb(string googlePrompt);
        Task<string> SummarizWebsite(string websiteUrl, string prompt);
    }
}