using Microsoft.Extensions.AI;

namespace RealynxBot.Services.Interfaces {
    internal interface ILmContexAwareness {
        Task<bool> ShouldRespond(string contextChannel);
        Task<bool> ShouldUseTools(string contextChannel);
    }
}