
namespace RealynxBot.Services.LLM {
    internal interface ILmContexAwareness {
        Task<bool> ShouldRespond(string contextChannel);
        Task<bool> ShouldUseTools(string contextChannel);
    }
}