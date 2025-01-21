

using Microsoft.Extensions.AI;

namespace RealynxBot.Services.Interfaces {
    internal interface ILmPersonalityService {
        string GetPersonalityPrompt { get; }

        void AddPersonalityContext(List<ChatMessage> languageModelContext);
    }
}