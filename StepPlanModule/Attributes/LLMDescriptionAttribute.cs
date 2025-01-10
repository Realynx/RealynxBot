namespace StepPlanModule.Attributes {
    public class LLMDescriptionAttribute : Attribute {
        public LLMDescriptionAttribute(string description) {
            Description = description;
        }

        public string Description { get; }
    }
}
