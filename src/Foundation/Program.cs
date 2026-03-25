using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Foundation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // EPiServer/Mediachase dependencies used by this sample still rely on BinaryFormatter.
            // Newer .NET runtimes disable it by default, so we enable it for local/dev usage.
            System.AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = environment == Environments.Development;

            if (isDevelopment)
            {
                Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File("App_Data/log.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();
            }


            CreateHostBuilder(args, isDevelopment).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args, bool isDevelopment)
        {
            if (isDevelopment)
            {
                return Host.CreateDefaultBuilder(args)
                    .ConfigureCmsDefaults()
                    .UseSerilog()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
            }
            else
            {
                return Host.CreateDefaultBuilder(args)
                    .ConfigureCmsDefaults()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
            }
        }
    }
}
