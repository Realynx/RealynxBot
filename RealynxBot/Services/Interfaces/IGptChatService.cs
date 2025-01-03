namespace RealynxBot.Services.Interfaces {
    public interface IGptChatService {
        Task<string> GenerateResponse(string prompt, string username);
        Task<string> SearchGoogle(string googleQuery);
    }
}