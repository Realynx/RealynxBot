

using Microsoft.Extensions.AI;

namespace RealynxBot.Services.Interfaces {
    internal interface ILmPersonalityService {
        void AddPersonalityContext(List<ChatMessage> languageModelContext);
    }
}