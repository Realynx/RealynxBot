using System.Reflection;

using Microsoft.Extensions.AI;

using RealynxBot.Services.LLM;

namespace RealynxBot.Services.Interfaces {
    internal interface ILmToolInvoker {
        IList<AITool> GetTools { get; }

        void AddDIPlugin<T>(T instance, MethodInfo function, AIFunctionFactoryCreateOptions? options = null);
        void AddPluginsOfType(Assembly pluginAssembly, LmToolInvoker.PlginType plginType);
        Task<string> LmToolCall(List<ChatMessage> chatMessages);
    }
}