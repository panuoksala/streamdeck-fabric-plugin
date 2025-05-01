using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamDeckLib;
using StreamDeckLib.DependencyInjection;
using StreamDeckLib.Hosting;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace StreamDeckMicrosoftFabric
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                        .ConfigureStreamDeckToolkit(args)
                        .ConfigureServices((hostContext, services) =>
                        {
                            services.AddHttpClient();
                            services.AddLogging((options) =>
                            {
                                options.ClearProviders();
                                options.AddConsole();
                                options.AddDebug();
                            });
                            services.AddStreamDeck(hostContext.Configuration, typeof(Program).Assembly);
                        });
    }
}
