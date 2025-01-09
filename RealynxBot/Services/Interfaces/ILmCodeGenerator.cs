
namespace RealynxBot.Services.LLM {
    public interface ILmCodeGenerator {
        Task<string> GenerateJs(string prompt);
    }
}