namespace RealynxBot.Services.Interfaces {
    public interface IGptChatService {
        Task<string> GenerateJs(string prompt);
        Task<string> GenerateResponse(string prompt, string username);
        Task<string> SearchGoogle(string googleQuery);
        Task<string> SummerizeWebsite(string websiteUrl, string prompt);
    }
}