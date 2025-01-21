using System.Reflection;

using Microsoft.Extensions.AI;

using RealynxBot.Services.LLM;

namespace RealynxBot.Services.Interfaces {
    internal interface ILmToolInvoker {
        IList<AITool> GetTools { get; }
        void AddPlugins(Type pluginType, object instance);
        Task<string> LmToolCall(List<ChatMessage> chatMessages);
    }
}