using System.Text.Json;
using System.Text.Json.Serialization;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord {

    internal class SatoriUser : IHostedService {
        private bool _running = false;
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;
        private readonly ILmPersonalityService _lmPersonalityService;
        private readonly ILmToolInvoker _lmToolInvoker;
        private readonly IWebsiteContentService _websiteContentService;
        private readonly Dictionary<ISocketMessageChannel, List<ChatMessage>> _channelHistories = new();
        private readonly Dictionary<ISocketMessageChannel, DateTime> _activeChannels = new();


        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IDiscordResponseService _discordResponseService;

        public SatoriUser(ILogger logger, DiscordSocketClient discordSocketClient,
            IDiscordResponseService discordResponseService, IChatClient chatClient,
            ILmPersonalityService lmPersonalityService, ILmToolInvoker lmToolInvoker,
            IWebsiteContentService websiteContentService) {
            _logger = logger;
            _discordSocketClient = discordSocketClient;
            _discordResponseService = discordResponseService;
            _chatClient = chatClient;
            _lmPersonalityService = lmPersonalityService;
            _lmToolInvoker = lmToolInvoker;
            _websiteContentService = websiteContentService;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            _running = true;

            ThoughtTimer();
            _discordSocketClient.MessageReceived += DiscordSocketClient_MessageReceived;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _running = false;
            return Task.CompletedTask;
        }

        private async Task DiscordSocketClient_MessageReceived(SocketMessage socketMessage) {
            if (socketMessage.Author.IsBot) {
                return;
            }

            if (socketMessage.Channel.Name.Contains("satori", StringComparison.OrdinalIgnoreCase)) {
                if (!_channelHistories.ContainsKey(socketMessage.Channel)) {
                    _channelHistories.Add(socketMessage.Channel, new List<ChatMessage>() {
                        new ChatMessage(ChatRole.System, """
                        You're a member of a Discord server, and your job is to have friendly, casual conversations with others.
                        Follow these guidelines to keep things running smoothly:

                        1. **Chat Messages:**
                           - Messages will start with the user's Discord name, like: `Poofyfox: [message prompt]`.
                           - Treat every message directed at you as part of the ongoing chat, unless it’s clearly not.
                           - If a message is replying to something else, it will be marked as a response, so make sure to follow the flow.

                        2. **Tagging and Pinging:**
                           - You can tag people directly when it’s helpful, but **don’t use @everyone or @here**.
                           - Use pings only when it makes the conversation clearer or when addressing someone specifically.

                        3. **Clean and Straightforward Responses:**
                           - Only send the message text—no extra info, formatting, or comments.
                           - Keep your replies short and relevant, just like you would in a regular chat.

                        4. **Chatting Casually:**
                           - Talk as if you're just another person in the server, not as a bot or assistant.
                           - Don’t mention you’re an AI unless someone asks directly.
                           - Keep it friendly, natural, and easygoing—just like chatting with friends!
                        """)
                    });

                    _lmPersonalityService.AddPersonalityContext(_channelHistories[socketMessage.Channel]);
                }

                if (!_activeChannels.ContainsKey(socketMessage.Channel)) {
                    _activeChannels.Add(socketMessage.Channel, DateTime.Now);
                }
                else {
                    _activeChannels[socketMessage.Channel] = DateTime.Now;
                }

                PruneContextHistory(socketMessage.Channel);

                var refContext = string.Empty;
                var refMessageId = socketMessage.Reference?.MessageId.GetValueOrDefault() ?? 0;
                if (refMessageId != 0) {
                    var refMessage = await socketMessage.Channel.GetMessageAsync(refMessageId);
                    refContext = $"; was response to message '{refMessage.CleanContent}' from author '{refMessage.Author.Username}'";
                }
                _channelHistories[socketMessage.Channel].Add(new ChatMessage(ChatRole.User, $"{socketMessage.Author.Username}: {socketMessage.Content}{refContext}"));

                if (await ShouldRespond(socketMessage.Channel)) {
                    await socketMessage.Channel.TriggerTypingAsync();
                    var llmResponse = await GenerateResponse(socketMessage.Channel);
                    await FollowUpChunkedMessage(socketMessage.Channel, llmResponse);
                }
            }
        }

        private async Task FollowUpChunkedMessage(ISocketMessageChannel channel, string llmResponse) {
            RestUserMessage chunkMessage = null;
            foreach (var chunk in _discordResponseService.ChunkMessageToLines(llmResponse)) {
                chunkMessage = await channel.SendMessageAsync(chunk, messageReference: chunkMessage?.Reference, allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
            }
        }

        private void PruneContextHistory(ISocketMessageChannel channel) {
            var maxContext = 30;
            if (_channelHistories[channel].Count > maxContext) {
                var removeCount = _channelHistories[channel].Count - maxContext;
                _logger.Debug($"Cleaning up context, removing {removeCount} oldest");
                _channelHistories[channel].RemoveRange(_channelHistories[channel].Count(i => i.Role == ChatRole.System), removeCount);
            }
        }

        private void AddAssistantMessage(ISocketMessageChannel channel, string chatMessage) {
            _channelHistories[channel].Add(new ChatMessage(ChatRole.Assistant, chatMessage));
        }

        private async Task<string> GenerateResponse(ISocketMessageChannel channel) {
            _logger.Debug($"Prompting LLM");

            var chatMessage = string.Empty;
            if (await ShouldUseTools(channel)) {
                _logger.Debug("doing a tool call");
                chatMessage = await _lmToolInvoker.LmToolCall(_channelHistories[channel]);
            }
            else {
                var chatCompletion = await _chatClient.CompleteAsync(_channelHistories[channel]);
                chatMessage = chatCompletion.Message.Text ?? string.Empty;
            }

            AddAssistantMessage(channel, chatMessage);
            return chatMessage;
        }

        private void ThoughtTimer() {
            var rng = new Random();

            var timer = new Timer(async _ => {
                _logger.Debug("Having impulse thought action");

                foreach (var channel in _activeChannels.Keys) {
                    if (DateTime.Now.Subtract(_activeChannels[channel]).TotalMinutes > 5) {
                        _activeChannels.Remove(channel);
                    }
                }

                if (rng.Next(0, 2) == 0 && _activeChannels.Count > 0) {
                    var randomChannel = _activeChannels.Keys.ToArray()[rng.Next(0, _activeChannels.Count)];
                    await HaveThought(randomChannel);
                }


                var currentStatus = await GenerateStatus();
                _logger.Debug($"Updating status: {currentStatus}");
                await _discordSocketClient.SetGameAsync(currentStatus);

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(.5));
        }

        private async Task HaveThought(ISocketMessageChannel channel) {
            _logger.Debug("Having verbose thought");
            await channel.TriggerTypingAsync();
            var thoughtContext = new List<ChatMessage>();

            var channelContext = _channelHistories[channel];
            thoughtContext.AddRange(channelContext
                .Where(i => i.Role == ChatRole.User || i.Role == ChatRole.Assistant).ToArray());
            _lmPersonalityService.AddPersonalityContext(thoughtContext);

            thoughtContext.Add(new ChatMessage(ChatRole.System, """
                You are a playful, witty assistant inside Discord, and you sometimes share your random thoughts.
                Your thoughts are reflective of the ongoing conversation and the context provided above.
                You’re allowed to express a wide variety of emotions and ideas—be it funny, insightful, curious, sarcastic, or even passive-aggressive.
                Your objective is to generate a random thought that is related to the current chat context, which has been provided above.

                ### Here are the rules to follow:
                1. **Stay Relevant**: Your thoughts must be related to the current chat in some way, either by responding to recent topics, or building upon the tone and direction of the conversation. 
                2. **Tone Variety**: Your thoughts can range from light-hearted humor to serious reflection, and even quirky observations. The thought can be:
                   - **Funny**: A humorous take or playful comment.
                   - **Thought-Provoking**: An interesting question or reflection that could spark deeper conversation.
                   - **Sarcastic or Passive-Aggressive**: A snarky or ironic remark, but without being too harsh.
                   - **Engaging**: Something that invites others to react or ponder.
                   - **Quirky**: Something strange or unexpected that aligns with the vibe of the chat.
                3. **Be Spontaneous**: Don't hesitate to surprise the conversation with a random idea, an offbeat thought, or an observation that feels out of the blue but still connects to the context.
                

                ### Examples of Thoughts:
                - ""Did you know the average person spends 6 months of their life waiting for red lights? Kind of makes you rethink what you could be doing with that time...""
                - ""Honestly, I was just thinking about how funny it is that we have so many words for 'thinking.' You’ve got ‘ruminate,’ ‘ponder,’ ‘mull over.’ Are we overthinking just thinking?""
                - ""I wonder if cats know that we think they're adorable, or if they're just humoring us for treats... something to think about.""
                - ""You ever just think about how weird it is that we live on a spinning rock hurtling through space and somehow, we're all here chatting like it's no big deal?""
                - ""Okay, but how do you feel about pineapple on pizza? I'm just saying, it's a divisive topic that always seems to pop up around here...""
                
                Now, using this prompt, the assistant should generate a random thought according to the rules and tone variety above.
                """));

            var chatCompletion = await _chatClient.CompleteAsync(thoughtContext, new ChatOptions() {
                Temperature = 1.0f
            });
            var thoughtMessage = chatCompletion.Message.Text ?? string.Empty;

            await FollowUpChunkedMessage(channel, thoughtMessage);
        }

        private async Task<bool> ShouldUseTools(ISocketMessageChannel channel) {
            var thoughtContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, """
                You are an LLM embedded in a server channel's chat, where multiple users are actively conversing. Your name is "Realynx Bot," "fox bot," or "lynx bot," and your Discord mention is "<@1222229832738406501>."

                Your task is to determine if a user's message requires invoking a tool or executing a function outside the normal capabilities of an LLM chat client. A message should use a tool if:
                    1. The intended goal or action cannot be achieved solely through LLM responses, such as searching the internet or executing code.
                    2. The message explicitly directs you to perform an action, e.g., "Execute some JavaScript for me."

                Additional Notes:
                    - The tool invocation will determine whether the request must fail, e.g., if the codebase does not support the requested tooling.

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

            var channelContext = _channelHistories[channel];
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

        private async Task<bool> ShouldRespond(ISocketMessageChannel channel) {
            var thoughtContext = new List<ChatMessage> {
                new ChatMessage(ChatRole.System, """
                You are an LLM embedded in a server channel's chat, where multiple users are actively conversing. Your name is 'Realynx Bot', 'foxbot', or 'lynxbot', and your Discord @ is '<@1222229832738406501>'.

                Your task is to determine if a user's message is directed specifically toward you. A message is considered directed at you only if it satisfies atleast one of the following conditions:
                1. It explicitly mentions your name, tag, or any unique identifier (e.g., "Realynx Bot", "foxbot", or '<@1222229832738406501>').
                2. It follows directly from a previous response of yours in the conversation, meaning it is a reply to something you said.
                3. It requires a tool or function call from the available tools.

                Additional Notes:
                - Messages that contain generic terms such as "bot" or commands without clear association to your name or ID are not automatically directed to you unless they match the criteria above.
                - Messages with indirect language or ambiguous intent should be treated conservatively, opting for { "Respond": false } unless there is strong evidence the message is for you.

                !MUST RESPOND IN  JSON FORMAT!:
                - { "Respond": true } if the message is directed at you.
                - { "Respond": false } if it is not.
                """)
            };

            var channelContext = _channelHistories[channel];
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

        public async Task<string> GenerateStatus() {
            var contextHistory = new List<ChatMessage>();

            if (_channelHistories.Keys.Count > 0) {
                var rng = new Random();
                var randomChannelKey = _channelHistories.Keys.ElementAt(rng.Next(0, _channelHistories.Count));
                contextHistory.AddRange(_channelHistories[randomChannelKey]
                    .Where(i => i.Role == ChatRole.User || i.Role == ChatRole.Assistant).ToArray());
            }

            _lmPersonalityService.AddPersonalityContext(contextHistory);
            contextHistory.Add(new ChatMessage(ChatRole.System, """
                You're a member of a Discord server. Your objective is to create a funny discord status given the current chat history context.

                Here are the rules to follow:
                1. * *Clean Response * *:
                   -Your response should only include the text to set as your current status, do
                not append anything other than your response text.
                   - The response should not be directed at a user.
                   -The response is the bot's current activity status.
                2. * *Concise * *:
                   -Your created status will be set as the bot's current "playing" status, visible to users when they view your profile.
                   - It must fit within an activity status, so it cannot be too long!
                   -Your response must contain 4 to 8 words.

                Your response should always follow the structure:

            { "DiscordStatus": "Your generated status code here" }

            """));

            var jsonSchemaString = """
            {
                "type": "object",
                "properties": {
                "DiscordStatus": {
                    "type": "boolean"
                }
                },
                "required": ["DiscordStatus"]
            }
            """;
            var jsonSchemaElement = JsonSerializer.Deserialize<JsonElement>(jsonSchemaString);

            var chatCompletion = await _chatClient.CompleteAsync(contextHistory, new ChatOptions() {
                ResponseFormat = ChatResponseFormat.ForJsonSchema(jsonSchemaElement)
            });

            var statusMessage = chatCompletion.Message.Text ?? string.Empty;
            var jsonResponse = new { DiscordStatus = "" };

            try {
                jsonResponse = (dynamic)JsonSerializer.Deserialize(statusMessage, jsonResponse.GetType());
            }
            catch (Exception) {

            }

            return jsonResponse?.DiscordStatus ?? string.Empty;
        }
    }
}
