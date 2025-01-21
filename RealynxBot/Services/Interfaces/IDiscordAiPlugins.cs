namespace RealynxBot.Services.Interfaces {
    internal interface IDiscordAiPlugins {
        Task<string> CreateServerInvite();
        Task CreateThread(string threadName);
        Task<string> GrabProfileInformation(string username);
        IList<string> ListChannels();
        Task MessageStatusUpdate(string status);
        Task UploadFile(string fileName, byte[] fileData);
    }
}