using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using System.Diagnostics;
using System.IO.Ports;

namespace SerialPortPool.Demo;

/// <summary>
/// Demo Orchestrator - Minimal working version with zero compilation errors
/// </summary>
public class DemoOrchestrator
{
    private readonly ILogger<DemoOrchestrator> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConsoleHelper _consoleHelper;
    private readonly ISerialPortPool _serialPortPool;
    private readonly ISerialPortDiscovery _discovery;
    private readonly IFtdiDeviceReader _ftdiReader;
    private readonly IMultiPortDeviceAnalyzer _deviceAnalyzer;
    private readonly Dictionary<string, object> _demoConfig;
    private readonly Stopwatch _applicationStopwatch;
    
    private int _totalWorkflowsExecuted = 0;
    private int _successfulWorkflows = 0;

    public DemoOrchestrator(
        ILogger<DemoOrchestrator> logger,
        IConfiguration configuration,
        ConsoleHelper consoleHelper,
        ISerialPortPool serialPortPool,
        ISerialPortDiscovery discovery,
        IFtdiDeviceReader ftdiReader,
        IMultiPortDeviceAnalyzer deviceAnalyzer,
        Dictionary<string, object> demoConfig)
    {
        _logger = logger;
        _configuration = configuration;
        _consoleHelper = consoleHelper;
        _serialPortPool = serialPortPool;
        _discovery = discovery;
        _ftdiReader = ftdiReader;
        _deviceAnalyzer = deviceAnalyzer;
        _demoConfig = demoConfig;
        _applicationStopwatch = Stopwatch.StartNew();
        
        // Simple logging - no method group issues
        _logger.LogInformation("DemoOrchestrator initialized successfully");
    }

    public async Task RunPythonDummyUUTDemoAsync()
    {
        var workflowResult = new WorkflowResult
        {
            WorkflowId = Guid.NewGuid().ToString("N")[..8],
            BibId = "bib_demo",
            UutId = "uut_python_simulator",
            PortNumber = 1,
            Protocol = "RS232",
            StartTime = DateTime.Now
        };

        try
        {
            _consoleHelper.ClearScreen();
            DisplayScenarioHeader("🤖 Python Dummy UUT Simulator", "Safe testing with simulated device");
            
            _consoleHelper.DisplayStatus("🔍 Checking Python dummy UUT availability...", ConsoleColor.Yellow);
            
            var expectedPort = "COM8";
            var isAvailable = await CheckDummyUUTAvailabilityAsync(expectedPort);
            
            if (!isAvailable)
            {
                await HandleDummyUUTNotAvailableAsync(expectedPort);
                workflowResult.Success = false;
                workflowResult.ErrorMessage = "Python dummy UUT not available on " + expectedPort;
                workflowResult.EndTime = DateTime.Now;
                _consoleHelper.DisplayWorkflowResult(workflowResult);
                return;
            }
            
            _consoleHelper.DisplaySuccess("Python dummy UUT detected on " + expectedPort);
            workflowResult.PortName = expectedPort;
            
            _consoleHelper.DisplayStatus("🔧 Testing SerialPortPool foundation services...", ConsoleColor.Cyan);
            await TestFoundationServicesAsync();
            
            _consoleHelper.DisplayStatus("🚀 Executing simulated 3-phase workflow...", ConsoleColor.Green);
            await ExecuteSimulated3PhaseWorkflowAsync(workflowResult, expectedPort);
            
            workflowResult.EndTime = DateTime.Now;
            workflowResult.Success = true;
            
            _totalWorkflowsExecuted++;
            _successfulWorkflows++;
            
            _consoleHelper.DisplayWorkflowResult(workflowResult);
            _consoleHelper.DisplaySuccess("🎉 Python dummy UUT demo completed successfully!");
            
        }
        catch (Exception ex)
        {
            // Simple logging - no interpolation issues
            _logger.LogError(ex, "Python dummy UUT demo failed");
            workflowResult.Success = false;
            workflowResult.ErrorMessage = ex.Message;
            workflowResult.EndTime = DateTime.Now;
            
            _totalWorkflowsExecuted++;
            
            _consoleHelper.DisplayError("Demo failed: " + ex.Message);
            _consoleHelper.DisplayWorkflowResult(workflowResult);
        }
    }

    public async Task RunRealHardwareDemoAsync()
    {
        try
        {
            _consoleHelper.ClearScreen();
            DisplayScenarioHeader("🏭 Real Hardware Detection", "Automatic FTDI device discovery and testing");
            
            _consoleHelper.DisplayStatus("🔍 Scanning for FTDI devices...", ConsoleColor.Yellow);
            var ftdiDevices = await DiscoverFtdiDevicesAsync();
            
            if (!ftdiDevices.Any())
            {
                _consoleHelper.DisplayWarning("No FTDI devices found");
                _consoleHelper.DisplayStatus("💡 Connect an FTDI device (FT4232H recommended) and try again", ConsoleColor.Blue);
                return;
            }
            
            DisplayDiscoveredDevices(ftdiDevices);
            _consoleHelper.DisplayStatus("🔀 Testing device grouping functionality...", ConsoleColor.Cyan);
            await TestDeviceGroupingAsync(ftdiDevices);
            
            _consoleHelper.DisplaySuccess("🎉 Real hardware demo completed!");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Real hardware demo failed");
            _consoleHelper.DisplayError("Real hardware demo failed: " + ex.Message);
        }
    }

    public async Task RunCustomConfigurationDemoAsync()
    {
        try
        {
            _consoleHelper.ClearScreen();
            DisplayScenarioHeader("🔧 Custom Configuration", "Manual port and parameter selection");
            
            var availablePorts = SerialPort.GetPortNames();
            if (!availablePorts.Any())
            {
                _consoleHelper.DisplayWarning("No serial ports detected");
                return;
            }
            
            var selectedPort = availablePorts[0];
            var baudRate = 115200;
            var timeout = 5000;
            
            Console.WriteLine("🔧 Custom Configuration (Simplified):");
            Console.WriteLine("   📍 Port: " + selectedPort);
            Console.WriteLine("   ⚡ Baud Rate: " + baudRate);
            Console.WriteLine("   ⏱️ Timeout: " + timeout + "ms");
            Console.WriteLine();
            
            _consoleHelper.DisplayStatus("🚀 Testing custom configuration...", ConsoleColor.Green);
            await TestCustomConfigurationAsync(selectedPort, baudRate, timeout);
            
            _consoleHelper.DisplaySuccess("🎉 Custom configuration demo completed!");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom configuration demo failed");
            _consoleHelper.DisplayError("Custom configuration demo failed: " + ex.Message);
        }
    }

    public async Task RunPerformanceTestDemoAsync()
    {
        try
        {
            _consoleHelper.ClearScreen();
            DisplayScenarioHeader("📊 Performance Testing", "Benchmarking and stress testing");
            
            var iterations = 10;
            
            _consoleHelper.DisplayStatus("🏃 Starting performance test (" + iterations + " iterations)...", ConsoleColor.Yellow);
            
            await TestPoolAllocationPerformanceAsync(iterations);
            await TestDiscoveryPerformanceAsync(iterations);
            
            _consoleHelper.DisplaySuccess("🎉 Performance testing completed!");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Performance testing failed");
            _consoleHelper.DisplayError("Performance testing failed: " + ex.Message);
        }
    }

    public async Task ShowSystemInformationAsync()
    {
        try
        {
            _consoleHelper.DisplayStatus("🔍 Gathering system information...", ConsoleColor.Yellow);
            
            var systemInfo = await CollectSystemInformationAsync();
            _consoleHelper.DisplaySystemInformation(systemInfo);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System information collection failed");
            _consoleHelper.DisplayError("System information collection failed: " + ex.Message);
        }
    }

    // ===================================
    // HELPER METHODS - ULTRA SIMPLE
    // ===================================

    private void DisplayScenarioHeader(string title, string description)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("📋 " + description);
        Console.WriteLine();
    }

    private async Task<bool> CheckDummyUUTAvailabilityAsync(string portName)
    {
        try
        {
            var availablePorts = SerialPort.GetPortNames();
            var portExists = availablePorts.Contains(portName);
            
            if (!portExists)
            {
                // Simple logging - no complex string operations
                _logger.LogWarning("Port not found: " + portName);
                return false;
            }
            
            using var serialPort = new SerialPort(portName, 115200);
            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            
            serialPort.Open();
            await Task.Delay(100);
            serialPort.Close();
            
            _logger.LogInformation("Port is available: " + portName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Port availability check failed: " + portName + " - " + ex.Message);
            return false;
        }
    }

    private async Task HandleDummyUUTNotAvailableAsync(string expectedPort)
    {
        _consoleHelper.DisplayError("Python dummy UUT not available on " + expectedPort);
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("💡 To run the Python dummy UUT demo:");
        Console.WriteLine();
        Console.WriteLine("1. Open a separate terminal/command prompt");
        Console.WriteLine("2. Navigate to the DummyUUT directory:");
        Console.WriteLine("   cd ../DummyUUT/");
        Console.WriteLine("3. Install Python dependencies:");
        Console.WriteLine("   pip install -r requirements.txt");
        Console.WriteLine("4. Start the dummy UUT simulator:");
        Console.WriteLine("   python dummy_uut.py --port " + expectedPort);
        Console.WriteLine("5. Return to this demo and try again");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        
        var availablePorts = SerialPort.GetPortNames();
        if (availablePorts.Any())
        {
            Console.WriteLine("📡 Available ports:");
            foreach (var port in availablePorts)
            {
                Console.WriteLine("   • " + port);
            }
        }
        else
        {
            Console.WriteLine("❌ No serial ports detected on this system");
        }
        
        await Task.Delay(100);
    }

    private async Task TestFoundationServicesAsync()
    {
        try
        {
            var stats = await _serialPortPool.GetStatisticsAsync();
            var statsMessage = "📊 Pool stats: " + stats.AvailablePorts.ToString() + " available, " + stats.AllocatedPorts.ToString() + " allocated";
            _consoleHelper.DisplayStatus(statsMessage, ConsoleColor.Green);
            
            var discoveryResult = await _discovery.DiscoverPortsAsync();
            var discoveryMessage = "🔍 Discovery found " + discoveryResult.Count().ToString() + " ports";
            _consoleHelper.DisplayStatus(discoveryMessage, ConsoleColor.Green);
            
            var deviceGroups = await _deviceAnalyzer.AnalyzeDeviceGroupsAsync(discoveryResult);
            var groupingMessage = "🔀 Device grouping found " + deviceGroups.Count().ToString() + " device groups";
            _consoleHelper.DisplayStatus(groupingMessage, ConsoleColor.Green);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Foundation services test failed");
            throw;
        }
    }

    private async Task ExecuteSimulated3PhaseWorkflowAsync(WorkflowResult result, string portName)
    {
        // Simulate PowerOn phase
        _consoleHelper.DisplayStatus("🔋 Phase 1: PowerOn (Simulated)", ConsoleColor.Blue);
        await Task.Delay(500);
        
        result.PowerOnResult = new PhaseResult
        {
            Success = true,
            TotalCommands = 1,
            SuccessfulCommands = 1,
            Duration = TimeSpan.FromMilliseconds(500),
            Commands = new List<CommandResult>
            {
                new CommandResult
                {
                    Command = "INIT_RS232",
                    Response = "READY",
                    Success = true,
                    ResponseTime = TimeSpan.FromMilliseconds(150)
                }
            }
        };
        
        _consoleHelper.DisplaySuccess("✅ PowerOn phase completed");
        
        // Simulate Test phase
        _consoleHelper.DisplayStatus("🧪 Phase 2: Test (Simulated)", ConsoleColor.Blue);
        await Task.Delay(800);
        
        result.TestResult = new PhaseResult
        {
            Success = true,
            TotalCommands = 1,
            SuccessfulCommands = 1,
            Duration = TimeSpan.FromMilliseconds(800),
            Commands = new List<CommandResult>
            {
                new CommandResult
                {
                    Command = "RUN_TEST_1",
                    Response = "PASS",
                    Success = true,
                    ResponseTime = TimeSpan.FromMilliseconds(200)
                }
            }
        };
        
        _consoleHelper.DisplaySuccess("✅ Test phase completed");
        
        // Simulate PowerOff phase
        _consoleHelper.DisplayStatus("🔌 Phase 3: PowerOff (Simulated)", ConsoleColor.Blue);
        await Task.Delay(300);
        
        result.PowerOffResult = new PhaseResult
        {
            Success = true,
            TotalCommands = 1,
            SuccessfulCommands = 1,
            Duration = TimeSpan.FromMilliseconds(300),
            Commands = new List<CommandResult>
            {
                new CommandResult
                {
                    Command = "STOP_RS232",
                    Response = "BYE",
                    Success = true,
                    ResponseTime = TimeSpan.FromMilliseconds(100)
                }
            }
        };
        
        _consoleHelper.DisplaySuccess("✅ PowerOff phase completed");
        
        // Performance metrics
        result.PerformanceMetrics = new PerformanceMetrics
        {
            AverageResponseTime = (150 + 200 + 100) / 3.0,
            FastestCommand = 100,
            SlowestCommand = 200,
            TotalCommunicationTime = 1600,
            OverheadTime = 200,
            ThroughputCommandsPerSecond = 3.0 / (1600 / 1000.0)
        };
    }

    private async Task<List<FtdiDeviceInfo>> DiscoverFtdiDevicesAsync()
    {
        var devices = new List<FtdiDeviceInfo>();
        
        try
        {
            var discoveryResult = await _discovery.DiscoverPortsAsync();
            var deviceGroups = await _deviceAnalyzer.AnalyzeDeviceGroupsAsync(discoveryResult);
            
            foreach (var group in deviceGroups)
            {
                if (group.DeviceId.Contains("VID_0403"))
                {
                    devices.Add(new FtdiDeviceInfo
                    {
                        Description = group.DeviceId,
                        DeviceId = group.DeviceId,
                        SerialNumber = group.SerialNumber ?? "Unknown",
                        IsMultiPort = group.Ports.Count > 1,
                        Ports = group.Ports.Select(p => p.PortName).ToList()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTDI device discovery failed");
        }
        
        return devices;
    }

    private void DisplayDiscoveredDevices(List<FtdiDeviceInfo> devices)
    {
        var countMessage = "Found " + devices.Count.ToString() + " FTDI device(s):";
        _consoleHelper.DisplaySuccess(countMessage);
        Console.WriteLine();
        
        foreach (var device in devices)
        {
            var icon = device.IsMultiPort ? "🔀" : "📌";
            var portCount = device.IsMultiPort ? "(" + device.Ports.Count.ToString() + " ports)" : "(1 port)";
            
            Console.WriteLine("   " + icon + " " + device.Description + " " + portCount);
            Console.WriteLine("     Device ID: " + device.DeviceId);
            Console.WriteLine("     Serial: " + device.SerialNumber);
            
            if (device.Ports.Any())
            {
                Console.WriteLine("     Ports: " + string.Join(", ", device.Ports));
            }
            
            Console.WriteLine();
        }
    }

    private async Task TestDeviceGroupingAsync(List<FtdiDeviceInfo> devices)
    {
        var multiPortDevices = devices.Where(d => d.IsMultiPort).ToList();
        
        if (multiPortDevices.Any())
        {
            var message = "✅ Multi-port device grouping working: " + multiPortDevices.Count.ToString() + " multi-port device(s)";
            _consoleHelper.DisplaySuccess(message);
            
            foreach (var device in multiPortDevices)
            {
                Console.WriteLine("   🔀 " + device.Description + ": " + string.Join(", ", device.Ports));
            }
        }
        else
        {
            _consoleHelper.DisplayStatus("📌 No multi-port devices found (single-port devices only)", ConsoleColor.Yellow);
        }
        
        await Task.Delay(100);
    }

    private async Task TestCustomConfigurationAsync(string port, int baudRate, int timeout)
    {
        try
        {
            using var serialPort = new SerialPort(port, baudRate);
            serialPort.ReadTimeout = timeout;
            serialPort.WriteTimeout = timeout;
            
            _consoleHelper.DisplayStatus("🔌 Opening " + port + " at " + baudRate + " baud...", ConsoleColor.Yellow);
            serialPort.Open();
            
            _consoleHelper.DisplaySuccess("✅ Port opened successfully");
            
            await Task.Delay(1000);
            
            serialPort.Close();
            _consoleHelper.DisplaySuccess("✅ Port closed successfully");
        }
        catch (Exception ex)
        {
            _consoleHelper.DisplayError("Custom configuration test failed: " + ex.Message);
        }
    }

    private async Task TestPoolAllocationPerformanceAsync(int iterations)
    {
        var successCount = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            _consoleHelper.DisplayProgress("Pool allocation test", i + 1, iterations);
            
            try
            {
                var allocation = await _serialPortPool.AllocatePortAsync(clientId: "PerfTest_" + i);
                
                if (allocation != null)
                {
                    successCount++;
                    await _serialPortPool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
                }
            }
            catch
            {
                // Ignore errors in performance test
            }
            
            await Task.Delay(10);
        }
        
        _consoleHelper.DisplaySuccess("✅ Pool allocation test: " + successCount + "/" + iterations + " successful");
    }

    private async Task TestDiscoveryPerformanceAsync(int iterations)
    {
        var successCount = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            _consoleHelper.DisplayProgress("Discovery performance test", i + 1, iterations);
            
            try
            {
                var result = await _discovery.DiscoverPortsAsync();
                successCount++;
            }
            catch
            {
                // Ignore errors in performance test
            }
            
            await Task.Delay(50);
        }
        
        _consoleHelper.DisplaySuccess("✅ Discovery test: " + successCount + "/" + iterations + " successful");
    }

    private async Task<SystemInfo> CollectSystemInformationAsync()
    {
        var systemInfo = new SystemInfo
        {
            ApplicationUptime = _applicationStopwatch.Elapsed,
            MemoryUsageMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            ThreadCount = Process.GetCurrentProcess().Threads.Count,
            FoundationServicesCount = 5,
            Sprint5ServicesReady = false,
            ConfigurationFilesCount = 1
        };
        
        var availablePorts = SerialPort.GetPortNames();
        foreach (var portName in availablePorts)
        {
            systemInfo.AvailablePorts.Add(new PortInfo
            {
                PortName = portName,
                Description = "Serial port " + portName,
                Manufacturer = "Unknown"
            });
        }
        
        try
        {
            var ftdiDevices = await DiscoverFtdiDevicesAsync();
            systemInfo.FtdiDevices.AddRange(ftdiDevices);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to collect FTDI device information: " + ex.Message);
        }
        
        return systemInfo;
    }
}