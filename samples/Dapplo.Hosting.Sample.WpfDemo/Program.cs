﻿using Dapplo.Microsoft.Extensions.Hosting.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Dapplo.Microsoft.Extensions.Hosting.AppServices;
using Dapplo.Microsoft.Extensions.Hosting.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Dapplo.Hosting.Sample.WpfDemo
{
    public static class Program
    {
        private const string AppSettingsFilePrefix = "appsettings";
        private const string HostSettingsFile = "hostsettings.json";
        private const string Prefix = "PREFIX_";

        [STAThread]
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging()
                .ConfigureConfiguration(args)
                .ConfigureSingleInstance(builder =>
                {
                    builder.MutexId = "{B9CE32C0-59AE-4AF0-BE39-5329AAFF4BE8}";
                    builder.WhenNotFirstInstance = (hostingEnvironment, logger) =>
                    {
                        // This is called when an instance was already started, this is in the second instance
                        logger.LogWarning("Application {0} already running.", hostingEnvironment.ApplicationName);
                    };
                })
                .ConfigurePlugins(pluginBuilder =>
                {
                    // Specify the location from where the Dll's are "globbed"
                    pluginBuilder.AddScanDirectories(Path.Combine(Directory.GetCurrentDirectory(), @"..\.."));
                    // Add the framework libraries which can be found with the specified globs
                    pluginBuilder.IncludeFrameworks(@"**\bin\**\*.FrameworkLib.dll");
                    // Add the plugins which can be found with the specified globs
                    pluginBuilder.IncludePlugins(@"**\bin\**\*.Plugin*.dll");
                })
                .ConfigureServices(serviceCollection =>
                {
                    // Make OtherWindow available for DI to MainWindow
                    serviceCollection.AddTransient<OtherWindow>();
                })
                .ConfigureWpf<MainWindow>()
                .UseWpfLifetime()
                .UseConsoleLifetime()
                .Build();

            Console.WriteLine("Run!");

            // This makes it possible to use RunAsync in the STA Thread
            // TODO: I'm not happy with this, might consider spending time for a different way
            SingleThreadedSynchronizationContext.Await(async () =>
            {
                await host.RunAsync();
            });
        }

        /// <summary>
        /// Configure the loggers
        /// </summary>
        /// <param name="hostBuilder">IHostBuilder</param>
        /// <returns>IHostBuilder</returns>
        private static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureLogging((hostContext, configLogging) =>
            {
                configLogging.AddConsole();
                configLogging.AddDebug();
            });
        }

        /// <summary>
        /// Configure the configuration
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IHostBuilder ConfigureConfiguration(this IHostBuilder hostBuilder, string[] args)
        {
            return hostBuilder.ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddJsonFile(HostSettingsFile, optional: true);
                configHost.AddEnvironmentVariables(prefix: Prefix);
                configHost.AddCommandLine(args);
            })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile(AppSettingsFilePrefix + ".json", optional: true);
                    if (!string.IsNullOrEmpty(hostContext.HostingEnvironment.EnvironmentName))
                    {
                        configApp.AddJsonFile(AppSettingsFilePrefix + $".{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    }
                    configApp.AddEnvironmentVariables(prefix: Prefix);
                    configApp.AddCommandLine(args);
                });
        }
    }
}