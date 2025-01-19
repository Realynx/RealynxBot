using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using RealynxServices.Config;

namespace RealynxServices.Extensions {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection ConfigureSkProviders(this IServiceCollection serviceDescriptors, OpenAiConfig openAiConfig) {
            var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                openAiConfig.InterpreterModelId,
                openAiConfig.ApiKey,
                openAiConfig.OrganizationId)
            .Build();

            var plugin = kernel
                .ImportPluginFromPromptDirectory(Path.Combine("SkServices", "SemanticPrompts"));

            serviceDescriptors
                .AddSingleton(kernel)
                .AddSingleton(kernel.GetRequiredService<IChatCompletionService>());

            return serviceDescriptors;
        }
    }
}
