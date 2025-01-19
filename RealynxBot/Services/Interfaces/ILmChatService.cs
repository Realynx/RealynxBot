namespace RealynxBot.Services.Interfaces {
    public interface ILmChatService {
        Task<string> GenerateResponse(string prompt, string username);
        Task<string> GenerateStatus();
    }
}