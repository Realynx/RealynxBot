using System.Data;
using System.Xml.Linq;

using Discord;
using Discord.Commands;
using Discord.Interactions;

using RealynxBot.Services.Discord.Interfaces;
using RealynxBot.Services.Interfaces;
using RealynxBot.Services.LLM;

using RealynxServices.Interfaces;

namespace RealynxBot.Services.Discord.Commands {
    public class OpenAiCommands : InteractionModuleBase<SocketInteractionContext> {
        private readonly ILmChatService _gptChatService;
        private readonly ILogger _logger;
        private readonly IDiscordResponseService _discordResponseService;
        private readonly ILmCodeGenerator _lmCodeGenerator;
        private readonly IHeadlessBrowserService _headlessBrowserService;
        private readonly ILmWebsiteAnalyzer _lmWebsiteAnalyzer;
        private readonly ILmCorrectGrammar _lmCorrectGrammar;
        private readonly ILmSpeechGenerator _lmSpeechGenerator;

        internal OpenAiCommands(ILmChatService gptChatService, ILogger logger, IDiscordResponseService discordResponseService,
            ILmCodeGenerator lmCodeGenerator, IHeadlessBrowserService headlessBrowserService, ILmWebsiteAnalyzer lmWebsiteAnalyzer,
            ILmCorrectGrammar lmCorrectGrammar, ILmSpeechGenerator lmSpeechGenerator) {
            _gptChatService = gptChatService;
            _logger = logger;
            _discordResponseService = discordResponseService;
            _lmCodeGenerator = lmCodeGenerator;
            _headlessBrowserService = headlessBrowserService;
            _lmWebsiteAnalyzer = lmWebsiteAnalyzer;
            _lmCorrectGrammar = lmCorrectGrammar;
            _lmSpeechGenerator = lmSpeechGenerator;
        }

        //[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        //[MessageCommand("TTS")]
        //public async Task TextToSpeech(IMessage message) {
        //    await DeferAsync();

        //    var audioData = await _lmSpeechGenerator.GenerateWavAudio(message.CleanContent);
        //    using var audioStream = new MemoryStream(audioData);

        //    var audioFile = new FileAttachment(audioStream, "tts-audio.wav");
        //    await FollowupWithFileAsync(audioFile);
        //}

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [MessageCommand("GrammarCheck")]
        public async Task CorrectGrammar(IMessage message) {
            await DeferAsync();

            var corrections = await _lmCorrectGrammar.CorrectGrammar(message.CleanContent);
            foreach (var chunk in _discordResponseService.ChunkMessageToLines(corrections)) {
                await FollowupAsync(chunk);
            }
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("gpt", "Replies with an AI generated response from LLM.")]
        public async Task PromptAi(string prompt) {
            await DeferAsync();

            try {
                var gptResponse = await _gptChatService.GenerateResponse(prompt, Context.User.Username);
                foreach (var chunk in _discordResponseService.ChunkMessageToLines(gptResponse)) {
                    await FollowupAsync(chunk);
                }
            }
            catch (Exception e) {
                await FollowupAsync("There was an error.");
                _logger.Error($"Error: {e}");
            }
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("function", "Tell GPT to generate a js function and execute it.")]
        public async Task GptFunction(string gptPrompt) {
            await DeferAsync();

            var gptJsCode = await _lmCodeGenerator.GenerateJs(gptPrompt);
            var consoleOutput = await _headlessBrowserService.ExecuteJs(gptJsCode);

            await _discordResponseService.ChunkMessage($"""
                '{gptPrompt}'
                ```javascript
                {gptJsCode}
                ```
                """, async message => await FollowupAsync(message));
            await _discordResponseService.ChunkMessage(string.Join("\n", consoleOutput), async message => await FollowupAsync(message));
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("google", "Uses GPT AI to query google and provide comprehensive results.")]
        public async Task SearchGoogle(string query) {
            await DeferAsync();

            try {
                var gptResponse = await _lmWebsiteAnalyzer.SearchWeb(query);
                foreach (var chunk in _discordResponseService.ChunkMessageToLines(gptResponse)) {
                    await FollowupAsync(chunk);
                }
            }
            catch (Exception e) {
                await FollowupAsync("There was an error.");
                _logger.Error($"Error: {e}");
            }
        }

        [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
        [SlashCommand("website", "Visit a host and summarize the page or response. Optionally you can add a prompt to the website.")]
        public async Task SummarizeWebsite(string websiteUrl, string question = "") {
            await DeferAsync();

            try {
                var gptResponse = await _lmWebsiteAnalyzer.SummarizWebsite(websiteUrl, question);
                foreach (var chunk in _discordResponseService.ChunkMessageToLines(gptResponse)) {
                    await FollowupAsync(chunk);
                }
            }
            catch (Exception e) {
                await FollowupAsync("There was an error.");
                _logger.Error($"Error: {e}");
            }
        }
    }
}
