using System.ComponentModel;
using System.Reflection;

using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmToolInvoker : ILmToolInvoker {
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;
        private readonly List<AITool> _aiFunctions = new List<AITool>();

        public LmToolInvoker(ILogger logger, OllamaToolClient ollamaToolClient) {
            _logger = logger;
            _chatClient = ollamaToolClient.ChatClient;
        }
        public IList<AITool> GetTools {
            get {
                return _aiFunctions;
            }
        }

        public void AddPluginsOfType(Assembly pluginAssembly) {
            ArgumentNullException.ThrowIfNull(pluginAssembly);

            foreach (var type in pluginAssembly.GetTypes()) {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
                    if (method is MethodInfo methodInfo && method.GetCustomAttribute<DescriptionAttribute>() is not null) {
                        var aiFunction = AIFunctionFactory.Create(methodInfo, null, null);
                        _aiFunctions.Add(aiFunction);
                    }
                }
            }

            Console.WriteLine($"Added {_aiFunctions.Count} AI functions");
        }

        public void AddPlugins(Type pluginType, object instance) {
            foreach (var method in pluginType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                if (method is MethodInfo methodInfo && method.GetCustomAttribute<DescriptionAttribute>() is not null) {
                    var aiFunction = AIFunctionFactory.Create(methodInfo, instance, null);
                    _aiFunctions.Add(aiFunction);
                }
            }

            Console.WriteLine($"Added {_aiFunctions.Count} AI functions for plugin type {pluginType.Name}");
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
