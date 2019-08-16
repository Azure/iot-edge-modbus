namespace Modbus.Containers
{
    using AzureIoTEdgeModbus;
    using AzureIoTEdgeModbus.Configuration;
    using AzureIoTEdgeModbus.DeviceTwin;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Wrappers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main()
        {
            var secondaryConfigFile = @"iot-edge-modbus.json";

            // Wait until the app unloads or is cancelled by external triggers, use it for exceptional scnearios only.
            using (var cts = new CancellationTokenSource())
            {
                // Wait until the app unloads or is cancelled
                AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                Console.CancelKeyPress += (sender, cpe) => cts.Cancel();

                // Bootstrap services using dependency injection.
                var services = new ServiceCollection();
                services.AddLogging(cfg => cfg.AddConsole());
                //services.AddLogging(cfg => cfg.AddApplicationInsights(Environment.GetEnvironmentVariable("ApplicationInsightsKey")))
                //    .Configure<LoggerFilterOptions>(cfg => cfg.MinLevel = LogLevel.Trace);

                services.AddSingleton<IModuleClient, ModuleClientWrapper>();
                services.AddSingleton<IEdgeModule, ModbusModule>();
                services.AddSingleton<ISessionsHandle, SessionsHandle>();
                services.AddSingleton<IDeviceConfiguration<ModuleConfig>, DeviceTwinConfiguration<ModuleConfig>>(
                                        sp => new DeviceTwinConfiguration<ModuleConfig>(
                                            sp.GetService<ILogger<ModbusModule>>(), File.Exists(secondaryConfigFile) ? new FileConfiguration<ModuleConfig>(sp.GetService<ILogger<ModbusModule>>(), File.OpenText(secondaryConfigFile)) : null, sp.GetService<IModuleClient>()));

                // Dispose method of ServiceProvider will dispose all disposable objects constructed by it as well.
                using (var serviceProvider = services.BuildServiceProvider())
                {
                    var logger = serviceProvider.GetService<ILogger<ModbusModule>>();

                    // Get a new module object.
                    using (var module = serviceProvider.GetService<IEdgeModule>())
                    {
                        using (var moduleClient = serviceProvider.GetService<IModuleClient>())
                        {
                            logger.LogInformation("IoT Hub module client and slave sessions initializing...");
                            await module.OpenConnectionAsync(serviceProvider.GetService<ISessionsHandle>(), cts.Token).ConfigureAwait(false);

                            WhenCancelled(cts.Token).Wait();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads.
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }
    }
}