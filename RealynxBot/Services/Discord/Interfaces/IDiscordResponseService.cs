namespace RealynxBot.Services.Discord.Interfaces {
    public interface IDiscordResponseService {
        IEnumerable<string> ChunkMessageToLines(string message);
        Task<bool> ChunkMessage(string largeMessage, Func<string, Task> followupAction);
    }
}