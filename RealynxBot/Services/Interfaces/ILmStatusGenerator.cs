
namespace RealynxBot.Services.LLM {
    internal interface ILmStatusGenerator {
        Task<string> GenerateStatus();
    }
}