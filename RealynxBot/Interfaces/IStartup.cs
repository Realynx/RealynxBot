using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RealynxBot.Interfaces {
    public interface IStartup {

        IConfiguration Configuration { get; set; }

        void ConfigureServices(IServiceCollection services);

        void Configure(IConfigurationBuilder configurationBuilder);
    }
}
