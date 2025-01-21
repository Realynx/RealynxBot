using System.Collections.Generic;
using System.Text;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM;
using RealynxBot.Services.LLM.ChatClients;

namespace RealynxBot.Services.Discord {

    internal class SatoriUser : IHostedService {
        private bool _running = false;
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;
        private readonly ILmPersonalityService _lmPersonalityService;
        private readonly ILmToolInvoker _lmToolInvoker;
        private readonly IGlobalChatContext _globalChatContext;
        private readonly ILmStatusGenerator _lmStatusGenerator;
        private readonly ILmContexAwareness _lmContexAwareness;
        private readonly ILmComputerVision _lmComputerVision;
        private readonly Dictionary<ISocketMessageChannel, DateTime> _activeChannels = new();


        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IDiscordResponseService _discordResponseService;

        public SatoriUser(ILogger logger, DiscordSocketClient discordSocketClient,
            IDiscordResponseService discordResponseService, OllamaUserChatClient ollamaChatClient,
            ILmPersonalityService lmPersonalityService, ILmToolInvoker lmToolInvoker,
            IGlobalChatContext globalChatContext, ILmStatusGenerator lmStatusGenerator,
            ILmContexAwareness lmContexAwareness, ILmComputerVision lmComputerVision) {
            _logger = logger;
            _discordSocketClient = discordSocketClient;
            _discordResponseService = discordResponseService;
            _lmPersonalityService = lmPersonalityService;
            _lmToolInvoker = lmToolInvoker;
            _globalChatContext = globalChatContext;
            _lmStatusGenerator = lmStatusGenerator;
            _lmContexAwareness = lmContexAwareness;
            _lmComputerVision = lmComputerVision;
            _chatClient = ollamaChatClient.ChatClient;
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

            await ExecuteSatoriLLM(socketMessage);
        }

        public async Task ExecuteSatoriLLM(SocketMessage socketMessage) {
            //if (socketMessage.Channel.Name.Contains("satori", StringComparison.OrdinalIgnoreCase)) {

            //}

            var channelId = socketMessage.Channel.Id.ToString();
            _globalChatContext.AddNewChat(channelId, $"""
                        You're a member of a Discord server, and your job is to have friendly, casual conversations with others.
                        Follow these guidelines to keep things running smoothly:

                        1. **Chat Messages:**
                           - Messages will start with the user's Discord name and time sent, like: `Poofyfox: [message prompt]`.
                           - Treat every message directed at you as part of the ongoing chat, unless it’s clearly not.
                           - If a message is replying to something else, it will be marked as a response, so make sure to follow the flow.

                        2. **Tagging and Pinging:**
                           - You can tag people directly when it’s helpful, but **don’t use @everyone or @here**.
                           - Use pings only when it makes the conversation clearer or when addressing someone specifically.

                        3. **Clean and Straightforward Responses:**
                           - Do not append the time to your messages.
                           - Only send the message text—no extra info, formatting, or comments.
                           - Keep your replies short and relevant, just like you would in a regular chat.

                        4. **Chatting Casually:**
                           - Talk as if you're just another person in the server, not as a bot or assistant.
                           - Don’t mention you’re an AI unless someone asks directly.
                           - Keep it friendly, natural, and easygoing—just like chatting with friends!

                           {_lmPersonalityService.GetPersonalityPrompt}
                        """);

            if (!_activeChannels.ContainsKey(socketMessage.Channel)) {
                _activeChannels.Add(socketMessage.Channel, DateTime.Now);
            }
            else {
                _activeChannels[socketMessage.Channel] = DateTime.Now;
            }

            var refContext = await GetRefrenceMessage(socketMessage);
            var userMessageContext = $"[{socketMessage.CreatedAt:T}] {socketMessage.Author.Username}: {socketMessage.CleanContent}{refContext}";
            _globalChatContext.AddMessage(channelId, new ChatMessage(ChatRole.User, userMessageContext));

            if (await _lmContexAwareness.ShouldRespond(socketMessage.Channel.Id.ToString())) {
                await socketMessage.Channel.TriggerTypingAsync();

                await AddImageContextData(socketMessage, channelId);

                var llmResponse = await GenerateResponse(socketMessage.Channel);
                await FollowUpChunkedMessage(socketMessage.Channel, llmResponse);
            }
        }

        private async Task AddImageContextData(SocketMessage socketMessage, string channelId) {
            var imageContext = new StringBuilder("""
            Below are the descriptions from the Image to Text LLM model of all the attached images:

            """);

            var attachments = new List<Attachment>();
            attachments.AddRange(socketMessage.Attachments);

            var refMessageId = socketMessage.Reference?.MessageId.GetValueOrDefault() ?? 0;
            if (refMessageId != 0) {
                var refMessage = await socketMessage.Channel.GetMessageAsync(refMessageId);
                attachments.AddRange(refMessage.Attachments.Select(i => (Attachment)i));
            }

            if (attachments.Count == 0) {
                return;
            }

            foreach (var attachment in attachments) {
                if (attachment.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase)) {
                    try {
                        var attachmentcontent = await new HttpClient().GetAsync(attachment.ProxyUrl);
                        attachmentcontent.EnsureSuccessStatusCode();

                        var attachmentBytes = await attachmentcontent.Content.ReadAsByteArrayAsync();
                        imageContext.AppendLine(await _lmComputerVision.DescribeImage(_globalChatContext[channelId], attachmentBytes, attachment.ContentType));
                    }
                    catch (Exception) {

                    }
                }
            }

            _globalChatContext.AddMessage(channelId, new ChatMessage(ChatRole.Assistant, imageContext.ToString()));

        }

        private async Task<string> GenerateResponse(ISocketMessageChannel channel) {
            string? chatMessage;
            if (await _lmContexAwareness.ShouldUseTools(channel.Id.ToString())) {
                _logger.Debug("doing a tool call");
                chatMessage = await _lmToolInvoker.LmToolCall(_globalChatContext[channel.Id.ToString()]);
            }
            else {
                chatMessage = await _globalChatContext.InfrenceChat(_chatClient, channel.Id.ToString());
            }

            return chatMessage;
        }

        private static async Task<string> GetRefrenceMessage(SocketMessage socketMessage) {
            var refContext = string.Empty;
            var refMessageId = socketMessage.Reference?.MessageId.GetValueOrDefault() ?? 0;
            if (refMessageId != 0) {
                var refMessage = await socketMessage.Channel.GetMessageAsync(refMessageId);
                refContext = $"; was response to message '{refMessage.CleanContent}' from author '{refMessage.Author.Username}'";
            }

            return refContext;
        }

        private async Task FollowUpChunkedMessage(ISocketMessageChannel channel, string llmResponse) {
            RestUserMessage chunkMessage = null;
            foreach (var chunk in _discordResponseService.ChunkMessageToLines(llmResponse)) {
                chunkMessage = await channel.SendMessageAsync(chunk, messageReference: chunkMessage?.Reference, allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
            }
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

                if (rng.Next(0, 4) == 0 && _activeChannels.Count > 0) {
                    var randomChannel = _activeChannels.Keys.ToArray()[rng.Next(0, _activeChannels.Count)];
                    await HaveThought(randomChannel);
                }


                var currentStatus = await _lmStatusGenerator.GenerateStatus();
                _logger.Debug($"Updating status: {currentStatus}");
                await _discordSocketClient.SetGameAsync(currentStatus);

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(.5));
        }


        private async Task HaveThought(ISocketMessageChannel channel) {
            _logger.Debug("Having verbose thought");
            await channel.TriggerTypingAsync();
            var thoughtContext = new List<ChatMessage>();

            var channelContext = _globalChatContext[channel.Id.ToString()];
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
    }
}
