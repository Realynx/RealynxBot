namespace RealynxBot.Services.Interfaces {
    public interface IHeadlessBrowserService {
        Task<byte[]> ScreenshotWebsite(string webAddress, bool fullLength = false);
    }
}