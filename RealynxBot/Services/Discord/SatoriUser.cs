
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;

namespace RealynxBot.Services.Discord {
    public class InfoModule : ModuleBase<SocketCommandContext> {
        // ~say hello world -> hello world
        [Command("say")]
        [Summary("Echoes a message.")]
        public Task SayAsync([Remainder][Summary("The text to echo")] string echo)
            => ReplyAsync(echo);

        // ReplyAsync is a method on ModuleBase 
    }


    internal class SatoriUser : IHostedService {
        private bool _running = false;
        private readonly ILogger _logger;
        private readonly IChatClient _chatClient;
        private readonly ILmPersonalityService _lmPersonalityService;
        private Dictionary<ISocketMessageChannel, List<ChatMessage>> _channelHistories = new();


        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IDiscordResponseService _discordResponseService;

        public SatoriUser(ILogger logger, DiscordSocketClient discordSocketClient,
            IDiscordResponseService discordResponseService, IChatClient chatClient, ILmPersonalityService lmPersonalityService) {
            _logger = logger;
            _discordSocketClient = discordSocketClient;
            _discordResponseService = discordResponseService;
            _chatClient = chatClient;
            _lmPersonalityService = lmPersonalityService;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            _running = true;

            ThoughtTimer();
            foreach (var guild in _discordSocketClient.Guilds) {
                //var satoriChannel = guild.GetTextChannel(1330564847330525285);
                //await satoriChannel.TriggerTypingAsync();

                var channels = guild.Channels.ToArray();
                Console.WriteLine($"{guild.Name} Channels: \n{string.Join("\n", channels.Select(i => i.Name))}");
            }

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
                        You are a chat assistant inside of discord. Your task is to chat with and help users. The following rules apply:
                        1. **Chat messages**:
                            - Chat messages will be prefixed with the user's discord name in example: 'Poofyfox: [message prompt]'.
                        2. **Tagging/Pinging**:
                            - DO NOT PING EVERYONE, You can ping individual users.
                        3. **Clean Response**:
                            - Your response should only include the text, do not append anything other then your response text.
                        """)
                    });

                    _lmPersonalityService.AddPersonalityContext(_channelHistories[socketMessage.Channel]);
                }

                var llmResponse = await GenerateResponse(socketMessage.Content, socketMessage.Author.Username, socketMessage.Channel);

                await FollowUpChunkedMessage(socketMessage.Channel, llmResponse);
            }
        }

        private async Task FollowUpChunkedMessage(ISocketMessageChannel channel, string llmResponse) {
            RestUserMessage chunkMessage = null;
            foreach (var chunk in _discordResponseService.ChunkMessageToLines(llmResponse)) {
                chunkMessage = await channel.SendMessageAsync(chunk, messageReference: chunkMessage?.Reference);
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

        private async Task<string> GenerateResponse(string prompt, string username, ISocketMessageChannel channel) {
            _logger.Debug($"Prompting LLM: '{prompt}'");

            PruneContextHistory(channel);
            _channelHistories[channel].Add(new ChatMessage(ChatRole.User, $"{username}: {prompt}"));

            var chatCompletion = await _chatClient.CompleteAsync(_channelHistories[channel]);
            var chatMessage = chatCompletion.Message.Text ?? string.Empty;

            AddAssistantMessage(channel, chatMessage);

            return chatMessage;
        }

        private void ThoughtTimer() {
            var thinkingThread = new Thread(async () => {
                while (_running) {
                    var rng = new Random();
                    var nextRandom = rng.Next(1, int.MaxValue);
                    if (nextRandom % 20 == 0) {
                        if (_channelHistories.Count > 0) {
                            var randomChannel = _channelHistories.Keys.ToArray()[rng.Next(0, _channelHistories.Keys.Count - 1)];
                            await HaveThought(randomChannel);
                        }
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            });
        }

        private async Task HaveThought(ISocketMessageChannel channel) {
            var thoughtContext = new List<ChatMessage>();

            var channelContext = _channelHistories[channel];
            thoughtContext.AddRange(channelContext
                .Where(i => i.Role == ChatRole.User || i.Role == ChatRole.Assistant).ToArray());
            _lmPersonalityService.AddPersonalityContext(channelContext);

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

            var chatCompletion = await _chatClient.CompleteAsync(channelContext, new ChatOptions() {
                Temperature = 1.0f
            });
            var thoughtMessage = chatCompletion.Message.Text ?? string.Empty;

            await FollowUpChunkedMessage(channel, thoughtMessage);
        }
    }
}
