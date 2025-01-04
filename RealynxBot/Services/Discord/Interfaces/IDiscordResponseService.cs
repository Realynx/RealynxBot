namespace RealynxBot.Services.Discord.Interfaces {
    public interface IDiscordResponseService {
        Task<bool> ChunkMessage(string largeMessage, Func<string, Task> followupAction);
    }
}