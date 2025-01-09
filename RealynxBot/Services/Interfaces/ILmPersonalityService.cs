
using OpenAI.Chat;

namespace RealynxBot.Services.Interfaces {
    internal interface ILmPersonalityService {
        void AddPersonalityContext(List<ChatMessage> languageModelContext);
    }
}