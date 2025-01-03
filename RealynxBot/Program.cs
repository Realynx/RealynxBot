using Microsoft.Extensions.Hosting;

using RealynxBot.Extensions;

namespace RealynxBot {
    internal static class Program {
        static async Task Main(string[] args) {
            var host = new HostBuilder()
                .UseConsoleLifetime()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
