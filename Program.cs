using System;
using System.ServiceProcess;
using NLog;
using System.Threading;
using System.Threading.Tasks;

namespace SerialPortPoolService
{
    public partial class SerialPortPoolService : ServiceBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource? cancellationTokenSource;
        private Task? serviceTask;

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
                logger.Info("SerialPortPoolService starting...");
                cancellationTokenSource = new CancellationTokenSource();
                serviceTask = Task.Run(async () => await RunServiceAsync(cancellationTokenSource.Token));
                logger.Info("SerialPortPoolService started successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to start SerialPortPoolService");
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                logger.Info("SerialPortPoolService stopping...");
                cancellationTokenSource?.Cancel();

                if (serviceTask != null && !serviceTask.Wait(TimeSpan.FromSeconds(30)))
                {
                    logger.Warn("Service task did not complete within timeout");
                }

                logger.Info("SerialPortPoolService stopped successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during service stop");
            }
            finally
            {
                cancellationTokenSource?.Dispose();
            }
        }

        private async Task RunServiceAsync(CancellationToken cancellationToken)
        {
            logger.Info("Service main loop starting");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    logger.Debug("Service heartbeat");
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                logger.Info("Service operation cancelled - normal shutdown");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error in service main loop");
                throw;
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
                    Console.WriteLine("Service running. Press any key to stop...");
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

            // Configuration simple sans EventLogTarget pour éviter les dépendances
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = config;
        }
    }
}
