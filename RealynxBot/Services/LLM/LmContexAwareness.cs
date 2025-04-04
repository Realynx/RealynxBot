﻿using System.Text.Json;

using Discord.WebSocket;

using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.LLM {
    internal class LmContexAwareness : ILmContexAwareness {
        private readonly ILogger _logger;
        private readonly IGlobalChatContext _globalChatContext;
        private readonly ILmToolInvoker _lmToolInvoker;
        private readonly IChatClient _chatClient;

        public LmContexAwareness(ILogger logger, IGlobalChatContext globalChatContext, OllamaUserChatClient chatClient, ILmToolInvoker lmToolInvoker) {
            _logger = logger;
            _globalChatContext = globalChatContext;
            _lmToolInvoker = lmToolInvoker;
            _chatClient = chatClient.ChatClient;
        }

        public async Task<bool> ShouldRespond(string contextChannel) {
            var thoughtContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, """
                You are an advanced language model integrated into a server channel's chat system. Your primary role is to determine whether a message from a user is directed specifically to you, the bot. Your unique identifiers are: "Realynx Bot", "foxbot", "lynxbot", or the Discord mention tag '<@1222229832738406501>'.

                A message should be considered directed at you if it satisfies **at least one** of the following conditions:
                1. The message explicitly mentions your name, unique identifiers, or tag (e.g., "Realynx Bot", "foxbot", "lynxbot", or '<@1222229832738406501>').
                2. The message is a direct continuation or reply to a response you provided earlier in the conversation.
                3. The message requires the use of a tool or function that only you can perform.

                ### Additional Rules:
                - Ignore messages that contain generic terms like "bot" unless they are clearly associated with your name, ID, or context from the conversation.
                - Ambiguous or indirect messages should **default to not responding** unless there is clear evidence that the user is addressing you.

                Your task:
                Analyze the conversation context and output a JSON object structured as follows:
                ```json
                {
                  "Respond": true // or false, based on whether the message is directed at you
                }
                """)
            };

            var channelContext = _globalChatContext[contextChannel];
            thoughtContext.AddRange(channelContext
                .Where(i => i.Role == ChatRole.User || i.Role == ChatRole.Assistant).ToArray());

            var jsonResponse = new { Respond = false };
            var jsonSchemaString = """
            {
                "type": "object",
                "properties": {
                "Respond": {
                    "type": "boolean"
                }
                },
                "required": ["Respond"]
            }
            """;
            var jsonSchemaelement = JsonSerializer.Deserialize<JsonElement>(jsonSchemaString);

            var chatCompletion = await _chatClient.CompleteAsync(thoughtContext, new ChatOptions() {
                MaxOutputTokens = 20,
                Temperature = 0f,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(jsonSchemaelement, "Should RespondObject"),
            });

            var thoughtMessage = chatCompletion.Message.Text ?? string.Empty;
            try {
                jsonResponse = (dynamic)JsonSerializer.Deserialize(thoughtMessage, jsonResponse.GetType());
            }
            catch (Exception) {

            }
            return jsonResponse?.Respond ?? false;
        }

        public async Task<bool> ShouldUseTools(string contextChannel) {
            var thoughtContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, """
                You are an LLM embedded in a server channel's chat, where multiple users are actively conversing.
                Your name is "Realynx Bot," "fox bot," or "lynx bot," and your Discord mention is "<@1222229832738406501>."

                Your task is to determine if a user's MOST RECENT message requires invoking a tool or executing a function outside the normal capabilities of an LLM chat client. 
                A message should use a tool if:
                    1. The intended goal or action cannot be achieved solely through LLM responses, such as searching the internet or executing code.
                    2. The message explicitly directs you to perform an action, e.g., "Execute some JavaScript for me."

                Additional Notes:
                    - The tool invocation will determine whether the request must fail, e.g., if the codebase does not support the requested tooling.
                    - If there are no tools that satify the prompt, then use tools.
                    - Do not use tools for image analysis.

                Respond in JSON format:
                - { "Tools": true } if the message is directed at you and requires a tool invocation.
                - { "Tools": false } if it does not require a tool invocation.

                """),
                new ChatMessage(ChatRole.System, $"""
                Available Tools:
                {string.Join("\n",
                    _lmToolInvoker.GetTools.Select(i=>$"Function Name: {((AIFunction)i).Metadata.Name} - Description: {((AIFunction)i).Metadata.Description}"))}

                """)
            };

            var channelContext = _globalChatContext[contextChannel];

            thoughtContext.AddRange(channelContext
                .Where(i => i.Role == ChatRole.User || i.Role == ChatRole.Assistant).ToArray());


            var jsonSchemaString = """
            {
                "type": "object",
                "properties": {
                "Tools": {
                    "type": "boolean"
                }
                },
                "required": ["Tools"]
            }
            """;
            var jsonSchemaElement = JsonSerializer.Deserialize<JsonElement>(jsonSchemaString);

            var chatCompletion = await _chatClient.CompleteAsync(thoughtContext, new ChatOptions() {
                MaxOutputTokens = 8,
                Temperature = 0f,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(jsonSchemaElement),
            });
            var thoughtMessage = chatCompletion.Message.Text ?? string.Empty;

            var jsonResponse = new { Tools = false };
            try {
                jsonResponse = (dynamic)JsonSerializer.Deserialize(thoughtMessage, jsonResponse.GetType());
            }
            catch (Exception) {

            }
            return jsonResponse?.Tools ?? false;
        }
    }
}
