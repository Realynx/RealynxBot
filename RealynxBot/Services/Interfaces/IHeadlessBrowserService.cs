namespace RealynxBot.Services.Interfaces {
    public interface IHeadlessBrowserService {
        Task<string[]> ExecuteJs(string js);
        Task<byte[]> ScreenshotWebsite(string webAddress, bool fullLength = false);
    }
}