using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

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

            var responseMessage = string.Empty;
            //for (var x = 0; x == 0 || await ShouldExecuteToolsAgain(toolPromptChain, x); x++) {
            _logger.Debug("Executing an AI cycle.");

            var chatClientResponse = await _chatClient.CompleteAsync(toolPromptChain, new ChatOptions() {
                Tools = _aiFunctions,
                ToolMode = ChatToolMode.RequireAny,
                Temperature = 0f
            }, new CancellationTokenSource().Token);

            responseMessage = chatClientResponse.Message.Text ?? string.Empty;
            //}

            return responseMessage;
        }


        private async Task<ChatMessage> FindRelavantContext(List<ChatMessage> contextChannel) {
            var thoughtContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, """
                You are an LLM responsible for executing tools (via function calls) within a codebase. 

                ### Task:
                - Reconstruct the relevant chat context.
                - Only use the most recent requst's context.
                - Use **clear breaklines** to separate information and make it easy for the tool LLM to understand.
                - Ensure the reconstructed context includes only the information necessary for completing the user's objective.
                - Use the same words and wording as the user prompt, we do not want to change the context.
                - DO NOT EDIT THE USER'S MESSAGES IN THE CONTEXT SIMPLE ADD OR OMIT;

                ### Output Format:
                DO NOT USE FORMATTING, OUTPUT JUST THE RAW CONTEXT.
                
                """)
            };

            thoughtContext.AddRange(contextChannel);
            var chatCompletion = await _chatClient.CompleteAsync(thoughtContext, new ChatOptions() {
                Temperature = 0.02f,
            });

            var newContext = chatCompletion.Message.Text ?? string.Empty;
            return new ChatMessage(ChatRole.System, newContext);
        }

        private async Task<bool> ShouldExecuteToolsAgain(List<ChatMessage> contextChannel, int cycles = 0) {
            if (cycles > 5) {
                return false;
            }

            var thoughtContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, """
                You are an LLM responsible for executing tools (function calls) within a codebase. You execute per cycle up to 8 cycles.
                Each cycle would represent the ability to call and pass function return values to other functions.
                Your task is to determine whether the tool runner should execute another cycle with the current set of variables in this context.

                Another cycle is required if any of the following conditions are met:
                1. The task remains incomplete and relies on the results of a function call already made (e.g., the function call was made, but its outcome is not complete or essential for progress).
                2. The user-requested task is not yet finished, and the reason for incompletion is not due to a hard limitation (e.g., resource or system constraints).
                """)
            };

            thoughtContext.AddRange(contextChannel);

            var jsonResponse = new { RunAgain = false };
            var jsonSchemaString = """
            {
                "type": "object",
                "properties": {
                "RunAgain": {
                    "type": "boolean"
                }
                },
                "required": ["RunAgain"]
            }
            """;
            var jsonSchemaelement = JsonSerializer.Deserialize<JsonElement>(jsonSchemaString);

            var chatCompletion = await _chatClient.CompleteAsync(thoughtContext, new ChatOptions() {
                MaxOutputTokens = 20,
                Temperature = 0f,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(jsonSchemaelement),
            });

            var thoughtMessage = chatCompletion.Message.Text ?? string.Empty;
            try {
                jsonResponse = (dynamic)JsonSerializer.Deserialize(thoughtMessage, jsonResponse.GetType());
            }
            catch (Exception) {

            }

            return jsonResponse?.RunAgain ?? false;
        }
    }
}
