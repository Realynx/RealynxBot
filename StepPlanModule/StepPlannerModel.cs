using Newtonsoft.Json;

namespace StepPlanModule {
    public class StepPlannerModel {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("steps")]
        public Steps Steps { get; set; }

        [JsonProperty("strict")]
        public bool Strict { get; set; }
    }

    public class Steps {
        [JsonProperty("functionCalls")]
        public Function[] FunctionCalls { get; set; }
    }

    public class Function {
        [JsonProperty("functionName")]
        public string FunctionName { get; set; }

        [JsonProperty("functionParameters")]
        public string[] FunctionParameters { get; set; }

        [JsonProperty("returnVariable")]
        public string ReturnVariable { get; set; }
    }
}
