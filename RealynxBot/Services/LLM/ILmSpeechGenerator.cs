
namespace RealynxBot.Services.LLM {
    internal interface ILmSpeechGenerator {
        Task<byte[]> GenerateWavAudio(string speechText);
    }
}