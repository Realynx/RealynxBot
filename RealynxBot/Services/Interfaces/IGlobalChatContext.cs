
using System.Text.Json;

using Microsoft.Extensions.AI;

namespace RealynxBot.Services.LLM {
    internal interface IGlobalChatContext {
        List<ChatMessage> this[string seed] { get; }

        List<ChatMessage>[] GetAllContexts { get; }

        void AddMessage(string identSeed, ChatMessage chatMessage);
        bool AddNewChat(string identSeed, string instructionPrompt);
        Task<string> ChatAndAdd(IChatClient chatClient, string identSeed, ChatMessage chatMessage);
        void ImportChatContext(string identSeed, List<ChatMessage> context);
        Task<string> InfrenceChat(IChatClient chatClient, string identSeed, JsonElement? schema = null);
    }
}