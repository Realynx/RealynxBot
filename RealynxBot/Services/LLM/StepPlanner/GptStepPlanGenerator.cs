using OpenAI.Chat;

using RealynxBot.Models.Config;
using RealynxBot.Services.Interfaces;

using StepPlanModule;

namespace RealynxBot.Services.LLM.StepPlanner {
    internal class GptStepPlanGenerator {
        private readonly ILogger _logger;
        private readonly OpenAiConfig _openAiConfig;
        private readonly ChatClient _chatClientGpt;


        public GptStepPlanGenerator(ILogger logger, OpenAiConfig openAiConfig) {
            _logger = logger;
            _openAiConfig = openAiConfig;
            _chatClientGpt = new(_openAiConfig.InterpreterModelId, _openAiConfig.ApiKey);
        }

        private List<ChatMessage> LanguageModelContext_CreateStepPlan(string objectivePrompt) {
            var queryContext = new List<ChatMessage> {
                new SystemChatMessage($"""
                    You are a large language model assistant, you revives objectives in the form of a user prompt and generate a step list of function calls for the program to complete.
                    The following rules apply:

                    **Function Calls**:
                        - Function's need parameters that you must extract from the user's intent with the user prompt/objective prompt.
                        - Function's can return values to a state variable that can be used to provide arguments for other function calls.
                    """),
            };
            return queryContext;
        }

        public async Task<StepPlan> PlanObjectiveSteps(string objectivePrompt, object functionModule) {
            var lmContext = LanguageModelContext_CreateStepPlan(objectivePrompt);

            var clientResult = await _chatClientGpt.CompleteChatAsync(lmContext, new ChatCompletionOptions() {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("StepPlannerSchema",
                BinaryData.FromString(File.ReadAllText(Path.Combine("JsonSchemas", "StepPlannerSchema.json")))),
            });

            var chatMessage = clientResult.Value.Content.FirstOrDefault()?.Text ?? "GPT refused to complete the chat";
        }
    }
}
