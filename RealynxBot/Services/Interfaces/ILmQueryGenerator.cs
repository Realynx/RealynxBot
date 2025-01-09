
namespace RealynxBot.Services.LLM {
    internal interface ILmQueryGenerator {
        Task<string> CreateQuery(string prompt);
    }
}