using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;

using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.LLM {
    internal class GlobalChatContext : IGlobalChatContext {
        private readonly Dictionary<Guid, List<ChatMessage>> _chatContexts = [];
        private readonly ILogger _logger;

        public GlobalChatContext(ILogger logger) {
            _logger = logger;
        }

        public List<ChatMessage>[] GetAllContexts {
            get {
                return _chatContexts.Values.ToArray();
            }
        }

        public List<ChatMessage> this[string seed] {
            get {
                var chatContext = GetChatContext(seed);
                return chatContext;
            }
        }

        public bool AddNewChat(string identSeed, string instructionPrompt) {
            var chatGuid = CreatePredictableGuid(identSeed);
            if (_chatContexts.ContainsKey(chatGuid)) {
                return false;
            }

            var chatContext = new List<ChatMessage>();
            _chatContexts.Add(chatGuid, chatContext);
            _logger.Debug($"Creating a new chat: '{identSeed}'");
            return true;
        }

        public void ImportChatContext(string identSeed, List<ChatMessage> context) {
            var chatContext = GetChatContext(identSeed);

            var importableContext = context
                .Where(i => i.Role == ChatRole.User || i.Role == ChatRole.Assistant).ToArray();

            chatContext.AddRange(importableContext);
        }

        public void AddMessage(string identSeed, ChatMessage chatMessage) {
            var chatContext = GetChatContext(identSeed);
            PruneChatHistory(identSeed);

            chatContext.Add(chatMessage);
        }

        public async Task<string> InfrenceChat(IChatClient chatClient, string identSeed, JsonElement? schema = null) {
            var chatContext = GetChatContext(identSeed);
            PruneChatHistory(identSeed);

            ChatCompletion chatCompletion;
            if (schema is JsonElement jsonSchemaElement) {
                _logger.Debug($"Infrencing Json LLM Request");
                chatCompletion = await chatClient.CompleteAsync(chatContext, new ChatOptions() {
                    ResponseFormat = ChatResponseFormat.ForJsonSchema(jsonSchemaElement)
                });
            }
            else {
                _logger.Debug($"Infrencing LLM Request");
                chatCompletion = await chatClient.CompleteAsync(chatContext);
            }

            var responseMessage = chatCompletion.Message.Text ?? string.Empty;
            chatContext.Add(new ChatMessage(ChatRole.Assistant, responseMessage));

            return responseMessage;
        }


        public async Task<string> ChatAndAdd(IChatClient chatClient, string identSeed, ChatMessage chatMessage) {
            var chatContext = GetChatContext(identSeed);
            PruneChatHistory(identSeed);

            chatContext.Add(chatMessage);
            _logger.Debug($"Prompting LLM: '{chatMessage.Text}'");
            var chatCompletion = await chatClient.CompleteAsync(chatContext);

            var responseMessage = chatCompletion.Message.Text ?? string.Empty;
            chatContext.Add(new ChatMessage(ChatRole.Assistant, responseMessage));

            return responseMessage;
        }

        private void PruneChatHistory(string identSeed, int maxLength = 15) {
            var chatContext = GetChatContext(identSeed);

            if (chatContext.Count > maxLength) {
                var removeCount = chatContext.Count - maxLength;
                _logger.Debug($"Cleaning up context, removing {removeCount} oldest");
                chatContext.RemoveRange(chatContext.Count(i => i.Role == ChatRole.System), removeCount);
            }
        }

        private List<ChatMessage> GetChatContext(string identSeed) {
            var guid = CreatePredictableGuid(identSeed);
            if (!_chatContexts.ContainsKey(guid)) {
                throw new KeyNotFoundException($"Chat history {identSeed} was not found!");
            }

            var chatContext = _chatContexts[guid];
            return chatContext;
        }

        private static Guid CreatePredictableGuid(string seed) {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));

            return new Guid(hash);
        }
    }
}
