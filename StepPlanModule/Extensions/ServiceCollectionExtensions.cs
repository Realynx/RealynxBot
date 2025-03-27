using Microsoft.Extensions.DependencyInjection;

namespace StepPlanModule.Extensions {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddStepPlanProvider(this IServiceCollection serviceDescriptors) {

            return serviceDescriptors;
        }
    }
}
