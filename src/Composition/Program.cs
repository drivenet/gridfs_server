using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GridFSServer.Composition
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var commandLineOptions = GetCommandLineOptions(args);
            var appConfiguration = LoadAppConfiguration(commandLineOptions.Config);
            while (true)
            {
                var hostingOptions = GetHostingOptions(commandLineOptions.HostingConfig);
                using (var host = BuildWebHost(hostingOptions, appConfiguration))
                {
                    host.Run();
                }
            }
        }

        private static IConfiguration LoadAppConfiguration(string configPath)
            => new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .Build();

        private static CommandLineOptions GetCommandLineOptions(string[] args)
            => new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build()
                .Get<CommandLineOptions>() ?? new CommandLineOptions();

        private static HostingOptions GetHostingOptions(string configPath)
            => new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false)
                .Build()
                .Get<HostingOptions>() ?? new HostingOptions();

        private static IWebHost BuildWebHost(HostingOptions hostingOptions, IConfiguration appConfiguration)
            => new WebHostBuilder()
                .UseSetting(WebHostDefaults.ServerUrlsKey, hostingOptions?.Listen)
                .ConfigureLogging(loggingBuilder => ConfigureLogging(loggingBuilder, hostingOptions))
                .UseKestrel(options => ConfigureKestrel(options, hostingOptions))
                .UseLibuv()
                .ConfigureServices(services => services.AddSingleton(appConfiguration))
                .UseStartup<Startup>()
                .Build();

        private static void ConfigureLogging(ILoggingBuilder loggingBuilder, HostingOptions hostingOptions)
        {
            if (loggingBuilder == null)
            {
                throw new ArgumentNullException(nameof(loggingBuilder));
            }

            if (hostingOptions == null)
            {
                throw new ArgumentNullException(nameof(hostingOptions));
            }

            loggingBuilder.AddFilter((category, level) => level >= LogLevel.Warning || level == LogLevel.Trace);
            var hasJournalD = Tmds.Systemd.Journal.IsSupported;
            if (hasJournalD)
            {
                loggingBuilder.AddJournal(options => options.SyslogIdentifier = "gridfs-server");
            }

            if (!hasJournalD || hostingOptions.ForceConsoleLogging)
            {
                loggingBuilder.AddConsole(options => options.DisableColors = true);
            }
        }

        private static void ConfigureKestrel(KestrelServerOptions options, HostingOptions hostingOptions)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 0;
            options.Limits.MaxRequestHeadersTotalSize = 16384;
            if (hostingOptions != null)
            {
                var maxConcurrentConnections = hostingOptions.MaxConcurrentConnections;
                if (maxConcurrentConnections != 0)
                {
                    options.Limits.MaxConcurrentConnections = maxConcurrentConnections;
                }
            }

            options.UseSystemd();
        }
    }
}
