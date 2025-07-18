using System;
using System.ServiceProcess;
using NLog;
using System.Threading;
using System.Threading.Tasks;
// NOUVEAU: Imports pour DI et Enhanced Discovery
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Models;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SerialPortPoolService
{
    public partial class SerialPortPoolService : ServiceBase
    {
        private static readonly NLog.Logger nlogLogger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource? cancellationTokenSource;
        private Task? serviceTask;
        
        // NOUVEAU: Service Provider pour DI
        private IServiceProvider? serviceProvider;

        public SerialPortPoolService()
        {
            ServiceName = "SerialPortPoolService";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                nlogLogger.Info("SerialPortPoolService starting with Enhanced Discovery integration...");
                
                // NOUVEAU: Setup Dependency Injection
                serviceProvider = SetupDependencyInjection();
                
                // Test que DI fonctionne (phase de dérisquage)
                TestDependencyInjection();
                
                cancellationTokenSource = new CancellationTokenSource();
                serviceTask = Task.Run(async () => await RunServiceAsync(cancellationTokenSource.Token));
                
                nlogLogger.Info("SerialPortPoolService started successfully with Enhanced Discovery");
            }
            catch (Exception ex)
            {
                nlogLogger.Error(ex, "Failed to start SerialPortPoolService with Enhanced Discovery");
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                nlogLogger.Info("SerialPortPoolService stopping...");
                cancellationTokenSource?.Cancel();

                if (serviceTask != null && !serviceTask.Wait(TimeSpan.FromSeconds(30)))
                {
                    nlogLogger.Warn("Service task did not complete within timeout");
                }

                // NOUVEAU: Cleanup DI
                if (serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                nlogLogger.Info("SerialPortPoolService stopped successfully");
            }
            catch (Exception ex)
            {
                nlogLogger.Error(ex, "Error during service stop");
            }
            finally
            {
                cancellationTokenSource?.Dispose();
            }
        }

        private async Task RunServiceAsync(CancellationToken cancellationToken)
        {
            nlogLogger.Info("Service main loop starting with Enhanced Discovery integration");

            try
            {
                // NOUVEAU: Démo Enhanced Discovery dans le service
                await TestEnhancedDiscoveryInService(cancellationToken);
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    nlogLogger.Debug("Service heartbeat with Enhanced Discovery available");
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                nlogLogger.Info("Service operation cancelled - normal shutdown");
            }
            catch (Exception ex)
            {
                nlogLogger.Error(ex, "Unexpected error in service main loop");
                throw;
            }
        }
        
        // NOUVEAU: Setup Dependency Injection
        private IServiceProvider SetupDependencyInjection()
        {
            var services = new ServiceCollection();
            
            // Logging - simple console logging pour DI (gardons NLog séparé)
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddConsole(); // Simple console logging pour Enhanced Discovery
            });
            
            // Configuration - détermine client vs dev selon environnement
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var isProduction = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
            
            if (isProduction)
            {
                services.AddSingleton(PortValidationConfiguration.CreateClientDefault());
                nlogLogger.Info("Using CLIENT configuration (strict 4232H validation)");
            }
            else
            {
                services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
                nlogLogger.Info("Using DEVELOPMENT configuration (permissive FTDI validation)");
            }
            
            // Enhanced Discovery Services
            services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
            services.AddScoped<ISerialPortValidator, SerialPortValidator>();
            services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
            
            nlogLogger.Info("Dependency Injection configured successfully");
            return services.BuildServiceProvider();
        }
        
        // NOUVEAU: Test que DI fonctionne (dérisquage)
        private void TestDependencyInjection()
        {
            try
            {
                var discovery = serviceProvider?.GetService<ISerialPortDiscovery>();
                var validator = serviceProvider?.GetService<ISerialPortValidator>();
                var ftdiReader = serviceProvider?.GetService<IFtdiDeviceReader>();
                
                if (discovery == null || validator == null || ftdiReader == null)
                {
                    throw new InvalidOperationException("Failed to resolve Enhanced Discovery services from DI container");
                }
                
                nlogLogger.Info("✅ Dependency Injection test successful - all Enhanced Discovery services resolved");
            }
            catch (Exception ex)
            {
                nlogLogger.Error(ex, "❌ Dependency Injection test failed");
                throw;
            }
        }
        
        // NOUVEAU: Test Enhanced Discovery dans le contexte du service
        private async Task TestEnhancedDiscoveryInService(CancellationToken cancellationToken)
        {
            try
            {
                nlogLogger.Info("Testing Enhanced Discovery integration in service context...");
                
                var discovery = serviceProvider?.GetRequiredService<ISerialPortDiscovery>();
                if (discovery == null)
                {
                    nlogLogger.Error("Failed to get Enhanced Discovery service");
                    return;
                }
                
                // Test discovery
                var ports = await discovery.DiscoverPortsAsync();
                var portList = ports.ToList();
                
                nlogLogger.Info($"Enhanced Discovery test: Found {portList.Count} serial ports");
                
                foreach (var port in portList)
                {
                    var ftdiStatus = port.IsFtdiDevice ? $"FTDI {port.FtdiChipType}" : "Non-FTDI";
                    var validStatus = port.IsValidForPool ? "VALID" : "INVALID";
                    
                    nlogLogger.Info($"  - {port.PortName}: {port.FriendlyName} [{ftdiStatus}] [{validStatus}]");
                    
                    if (port.IsFtdiDevice && port.FtdiInfo != null)
                    {
                        nlogLogger.Info($"    FTDI Details: VID/PID={port.FtdiInfo.VendorId}/{port.FtdiInfo.ProductId}, Serial={port.FtdiInfo.SerialNumber}");
                    }
                    
                    if (port.ValidationResult != null)
                    {
                        nlogLogger.Info($"    Validation: {port.ValidationResult.Reason} (Score: {port.ValidationResult.ValidationScore}%)");
                    }
                }
                
                nlogLogger.Info("✅ Enhanced Discovery integration test completed successfully");
            }
            catch (Exception ex)
            {
                nlogLogger.Error(ex, "❌ Enhanced Discovery integration test failed");
                // Ne pas throw - le service doit continuer même si discovery échoue
            }
        }

        public void StartInteractive() => OnStart(Array.Empty<string>());
        public void StopInteractive() => OnStop();
    }

    static class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogging();

            try
            {
                if (Environment.UserInteractive)
                {
                    var service = new SerialPortPoolService();
                    service.StartInteractive();
                    Console.WriteLine("Service running with Enhanced Discovery integration. Press any key to stop...");
                    Console.ReadKey();
                    service.StopInteractive();
                }
                else
                {
                    ServiceBase.Run(new SerialPortPoolService());
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Fatal(ex, "Fatal error in service startup");
                throw;
            }
        }

        private static void ConfigureLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = @"C:\Logs\SerialPortPool\service-${shortdate}.log",
                Layout = "${longdate} ${uppercase:${level}} ${logger} ${message} ${exception:format=tostring}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 10
            };

            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);
            LogManager.Configuration = config;
        }
    }
}