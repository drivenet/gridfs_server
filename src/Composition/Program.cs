﻿using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tmds.Systemd;

namespace GridFSServer.Composition
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var commandLineOptions = GetCommandLineOptions(args);
            var appConfiguration = LoadAppConfiguration(commandLineOptions.Config);

            do
            {
                var hostingOptions = GetHostingOptions(commandLineOptions.HostingConfig);
                using (var host = BuildWebHost(hostingOptions, appConfiguration))
                {
                    await host.RunAsync();
                }
            }
            while (ServiceManager.IsRunningAsService);
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
            if (loggingBuilder is null)
            {
                throw new ArgumentNullException(nameof(loggingBuilder));
            }

            if (hostingOptions is null)
            {
                throw new ArgumentNullException(nameof(hostingOptions));
            }

            loggingBuilder.AddFilter((category, level) => level >= LogLevel.Warning || level == LogLevel.Trace);
            var hasJournalD = Journal.IsSupported;
            if (hasJournalD)
            {
                loggingBuilder.AddJournal(options =>
                {
                    options.SyslogIdentifier = "gridfs-server";
                    options.DropWhenBusy = true;
                });
            }

            if (!hasJournalD || hostingOptions.ForceConsoleLogging)
            {
                loggingBuilder.AddConsole(options => options.DisableColors = true);
            }
        }

        private static void ConfigureKestrel(KestrelServerOptions options, HostingOptions hostingOptions)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 0;
            options.Limits.MaxRequestHeadersTotalSize = 16384;

            // To match all single-chunk files
            options.Limits.MaxResponseBufferSize = 257 << 10;
            if (hostingOptions is object)
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
