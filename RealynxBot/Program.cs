using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

using RealynxBot.Extensions;

namespace RealynxBot {
    internal static class Program {
        static async Task Main(string[] args) {
            var host = Kernel
                .CreateBuilder()
                .UseStartup<Startup>()
                .Build();

            await host.RunAsync();
        }
    }
}
