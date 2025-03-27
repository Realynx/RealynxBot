using StepPlanModule.Attributes;

namespace StepPlanModule.General {
    public class BasicFunctions {
        [LLMDescription("Multiply int A with int B and return int of the sum.")]
        public int Sum([LLMDescription("Int a")] int a,
            [LLMDescription("Int b")] int b) {

            return a * b;
        }
    }
}
