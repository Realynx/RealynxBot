using Microsoft.Extensions.AI;

using System.ComponentModel;

using RealynxBot.Services.Interfaces;
using System.Reflection;
using System.Net;
using System.Collections.Generic;

namespace RealynxBot.Services.LLM {
    internal class LmToolInvoker : ILmToolInvoker {
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;
        private readonly List<AITool> _aiFunctions = new List<AITool>();

        public LmToolInvoker(ILogger logger) {
            _logger = logger;

            _chatClient = new OllamaChatClient("http://10.0.1.123", modelId: "llama3-groq-tool-use:8b")
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
        }
        public IList<AITool> GetTools {
            get {
                return _aiFunctions;
            }
        }

        public enum PlginType {
            Common = 0,
            Discord,
            Misc
        }

        public void AddPluginsOfType(Assembly pluginAssembly, PlginType plginType) {
            ArgumentNullException.ThrowIfNull(pluginAssembly);

            foreach (var type in pluginAssembly.GetTypes()) {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
                    if (method is MethodInfo methodInfo && method.GetCustomAttribute<DescriptionAttribute>() is not null) {
                        var aiFunction = AIFunctionFactory.Create(methodInfo, null, null);
                        _aiFunctions.Add(aiFunction);
                    }
                }
            }

            Console.WriteLine($"Added {_aiFunctions.Count} AI functions for plugin type {plginType}");
        }

        public void AddDIPlugin<T>(T instance, MethodInfo function, AIFunctionFactoryCreateOptions? options = null) {
            var aiFunction = AIFunctionFactory.Create(function, instance, options);
            _aiFunctions.Add(aiFunction);
        }

        public async Task<string> LmToolCall(List<ChatMessage> chatMessages) {
            var toolPromptChain = chatMessages.ToArray().ToList();
            var chatClientResponse = await _chatClient.CompleteAsync(toolPromptChain, new ChatOptions() {
                Tools = _aiFunctions,
                ToolMode = ChatToolMode.RequireAny,
                Temperature = 0f
            });

            return chatClientResponse.Message.Text ?? string.Empty;
        }
    }
}
