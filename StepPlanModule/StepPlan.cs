using System.Reflection;

namespace StepPlanModule {
    public class StepPlan {
        public Assembly[] StepModules { get; set; }
        public StepPlannerModel PlanModel { get; set; }
        public Dictionary<string, object> State { get; set; }

        public async Task ExecuteSteps(Dictionary<string, object> stateValue = null) {
            if (stateValue is not null) {
                State = stateValue;
            }


        }
    }
}
