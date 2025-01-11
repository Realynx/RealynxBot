using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

using RealynxBot.Interfaces;

namespace RealynxBot.Extensions {
    public static class HostBuilderExtension {
        public static IKernelBuilder UseStartup<T>(this IKernelBuilder hostBuilder) where T : IStartup, new() {
            IStartup startup = new T();

            var configBuilder = new ConfigurationBuilder();
            startup.Configure(configBuilder);

            var config = configBuilder.Build();
            startup.Configuration = config;

            startup.ConfigureServices(hostBuilder.Services);

            return hostBuilder;
        }
    }
}
