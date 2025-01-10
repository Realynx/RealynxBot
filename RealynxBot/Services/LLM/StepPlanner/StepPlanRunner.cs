using RealynxServices.Interfaces;

using StepPlanModule;

namespace RealynxBot.Services.LLM.StepPlanner {
    internal class StepPlanRunner {
        private readonly ILogger _logger;

        public StepPlanRunner(ILogger logger) {
            _logger = logger;
        }

        public async Task ExecutePlan(StepPlan stepPlan) {

        }
    }
}
