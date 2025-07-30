using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.Json;

namespace SerialPortPool.Demo;

/// <summary>
/// Demo Orchestrator - Complete workflow engine for RS232Demo scenarios
/// Manages all demo scenarios with rich console output and comprehensive error handling
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
    
    // Performance tracking
    private readonly Dictionary<string, List<double>> _performanceMetrics = new();
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
        
        _logger.LogInformation("🎬 DemoOrchestrator initialized successfully");
    }

    /// <summary>
    /// Demo Scenario 1: Python Dummy UUT Simulator
    /// Safe testing with predictable responses
    /// </summary>
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
            
            // Step 1: Validate dummy UUT availability
            _consoleHelper.DisplayStatus("🔍 Checking Python dummy UUT availability...", ConsoleColor.Yellow);
            
            var expectedPort = _configuration.GetValue<string>("Demo:PythonDummyUUT:ExpectedPort") ?? "COM8";
            var isAvailable = await CheckDummyUUTAvailabilityAsync(expectedPort);
            
            if (!isAvailable)
            {
                await HandleDummyUUTNotAvailableAsync(expectedPort);
                workflowResult.Success = false;
                workflowResult.ErrorMessage = $"Python dummy UUT not available on {expectedPort}";
                workflowResult.EndTime = DateTime.Now;
                _consoleHelper.DisplayWorkflowResult(workflowResult);
                return;
            }
            
            _consoleHelper.DisplaySuccess($"Python dummy UUT detected on {expectedPort}");
            workflowResult.PortName = expectedPort;
            
            // Step 2: Test basic pool functionality
            _consoleHelper.DisplayStatus("🔧 Testing SerialPortPool foundation services...", ConsoleColor.Cyan);
            await TestFoundationServicesAsync();
            
            // Step 3: Simulate 3-phase workflow (simplified for Sprint 5 foundation)
            _consoleHelper.DisplayStatus("🚀 Executing simulated 3-phase workflow...", ConsoleColor.Green);
            await ExecuteSimulated3PhaseWorkflowAsync(workflowResult, expectedPort);
            
            // Step 4: Display results
            workflowResult.EndTime = DateTime.Now;
            workflowResult.Success = true;
            
            _totalWorkflowsExecuted++;
            _successfulWorkflows++;
            
            _consoleHelper.DisplayWorkflowResult(workflowResult);
            
            _consoleHelper.DisplaySuccess("🎉 Python dummy UUT demo completed successfully!");
            _consoleHelper.DisplayStatus("📋 This demo validates the foundation for Sprint 5 services", ConsoleColor.Blue);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Python dummy UUT demo failed");
            workflowResult.Success = false;
            workflowResult.ErrorMessage = ex.Message;
            workflowResult.EndTime = DateTime.Now;
            
            _totalWorkflowsExecuted++;
            
            _consoleHelper.DisplayError($"Demo failed: {ex.Message}");
            _consoleHelper.DisplayWorkflowResult(workflowResult);
        }
    }

    /// <summary>
    /// Demo Scenario 2: Real Hardware Detection and Testing
    /// </summary>
    public async Task RunRealHardwareDemoAsync()
    {
        try
        {
            _consoleHelper.ClearScreen();
            DisplayScenarioHeader("🏭 Real Hardware Detection", "Automatic FTDI device discovery and testing");
            
            // Step 1: Discover FTDI devices
            _consoleHelper.DisplayStatus("🔍 Scanning for FTDI devices...", ConsoleColor.Yellow);
            var ftdiDevices = await DiscoverFtdiDevicesAsync();
            
            if (!ftdiDevices.Any())
            {
                _consoleHelper.DisplayWarning("No FTDI devices found");
                _consoleHelper.DisplayStatus("💡 Connect an FTDI device (FT4232H recommended) and try again", ConsoleColor.Blue);
                return;
            }
            
            // Step 2: Display discovered devices
            DisplayDiscoveredDevices(ftdiDevices);
            
            // Step 3: Test multi-port device grouping
            _consoleHelper.DisplayStatus("🔀 Testing device grouping functionality...", ConsoleColor.Cyan);
            await TestDeviceGroupingAsync(ftdiDevices);
            
            // Step 4: Test pool allocation with real devices
            _consoleHelper.DisplayStatus("🔒 Testing port allocation with real hardware...", ConsoleColor.Green);
            await TestRealHardwareAllocationAsync(ftdiDevices);
            
            _consoleHelper.DisplaySuccess("🎉 Real hardware demo completed!");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Real hardware demo failed");
            _consoleHelper.DisplayError($"Real hardware demo failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demo Scenario 3: Custom Configuration
    /// </summary>
    public async Task RunCustomConfigurationDemoAsync()
    {
        try
        {
            _consoleHelper.ClearScreen();
            DisplayScenarioHeader("🔧 Custom Configuration", "Manual port and parameter selection");
            
            // Step 1: Get available ports
            var availablePorts = SerialPort.GetPortNames();
            if (!availablePorts.Any())
            {
                _consoleHelper.DisplayWarning("No serial ports detected");
                return;
            }
            
            // Step 2: Interactive port selection
            var selectedPort = GetUserPortSelection(availablePorts);
            if (string.IsNullOrEmpty(selectedPort))
            {
                _consoleHelper.DisplayStatus("Demo cancelled by user", ConsoleColor.Yellow);
                return;
            }
            
            // Step 3: Get configuration parameters
            var baudRate = GetUserBaudRateSelection();
            var timeout = GetUserTimeoutSelection();
            
            // Step 4: Display selected configuration
            DisplayCustomConfiguration(selectedPort, baudRate, timeout);
            
            // Step 5: Test custom configuration
            _consoleHelper.DisplayStatus("🚀 Testing custom configuration...", ConsoleColor.Green);
            await TestCustomConfigurationAsync(selectedPort, baudRate, timeout);
            
            _consoleHelper.DisplaySuccess("🎉 Custom configuration demo completed!");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Custom configuration demo failed");
            _consoleHelper.DisplayError($"Custom configuration demo failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demo Scenario 4: Performance Testing
    /// </summary>
    public async Task RunPerformanceTestDemoAsync()
    {
        try
        {
            _consoleHelper.ClearScreen();
            DisplayScenarioHeader("📊 Performance Testing", "Benchmarking and stress testing");
            
            var iterations = _configuration.GetValue<int>("Demo:PerformanceTesting:DefaultIterations");
            var enableStress = _configuration.GetValue<bool>("Demo:PerformanceTesting:EnableStressTest");
            
            _consoleHelper.DisplayStatus($"🏃 Starting performance test ({iterations} iterations)...", ConsoleColor.Yellow);
            
            // Step 1: Pool allocation performance
            await TestPoolAllocationPerformanceAsync(iterations);
            
            // Step 2: Discovery performance
            await TestDiscoveryPerformanceAsync(iterations);
            
            // Step 3: Concurrent operations (if enabled)
            if (enableStress)
            {
                await TestConcurrentOperationsAsync();
            }
            
            // Step 4: Display performance metrics
            DisplayPerformanceMetrics();
            
            _consoleHelper.DisplaySuccess("🎉 Performance testing completed!");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Performance testing failed");
            _consoleHelper.DisplayError($"Performance testing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demo Scenario 5: System Information Display
    /// </summary>
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
            _logger.LogError(ex, "❌ System information collection failed");
            _consoleHelper.DisplayError($"System information collection failed: {ex.Message}");
        }
    }

    // ===================================
    // HELPER METHODS
    // ===================================

    /// <summary>
    /// Display scenario header with description
    /// </summary>
    private void DisplayScenarioHeader(string title, string description)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"📋 {description}");
        Console.WriteLine();
    }

    /// <summary>
    /// Check if Python dummy UUT is available on specified port
    /// </summary>
    private async Task<bool> CheckDummyUUTAvailabilityAsync(string portName)
    {
        try
        {
            var availablePorts = SerialPort.GetPortNames();
            var portExists = availablePorts.Contains(portName);
            
            if (!portExists)
            {
                _logger.LogWarning("Port {PortName} not found in available ports: {AvailablePorts}",
                    portName, string.Join(", ", availablePorts));
                return false;
            }
            
            // Try to open port briefly to check availability
            using var serialPort = new SerialPort(portName, 115200);
            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            
            serialPort.Open();
            await Task.Delay(100); // Brief connection test
            serialPort.Close();
            
            _logger.LogInformation("✅ Port {PortName} is available and accessible", portName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ Port {PortName} availability check failed: {Error}", portName, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Handle dummy UUT not available scenario
    /// </summary>
    private async Task HandleDummyUUTNotAvailableAsync(string expectedPort)
    {
        _consoleHelper.DisplayError($"Python dummy UUT not available on {expectedPort}");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("💡 To run the Python dummy UUT demo:");
        Console.WriteLine();
        Console.WriteLine("1. Open a separate terminal/command prompt");
        Console.WriteLine("2. Navigate to the DummyUUT directory:");
        Console.WriteLine("   cd ../DummyUUT/");
        Console.WriteLine("3. Install Python dependencies (if not done already):");
        Console.WriteLine("   pip install -r requirements.txt");
        Console.WriteLine("4. Start the dummy UUT simulator:");
        Console.WriteLine($"   python dummy_uut.py --port {expectedPort}");
        Console.WriteLine("5. Return to this demo and try again");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        
        // Show available ports as alternative
        var availablePorts = SerialPort.GetPortNames();
        if (availablePorts.Any())
        {
            Console.WriteLine("📡 Available ports (you can modify the dummy UUT to use these):");
            foreach (var port in availablePorts)
            {
                Console.WriteLine($"   • {port}");
            }
        }
        else
        {
            Console.WriteLine("❌ No serial ports detected on this system");
        }
        
        await Task.Delay(100);
    }

    /// <summary>
    /// Test foundation services functionality
    /// </summary>
    private async Task TestFoundationServicesAsync()
    {
        try
        {
            // Test pool statistics
            var stats = await _serialPortPool.GetStatisticsAsync();
            _consoleHelper.DisplayStatus($"📊 Pool stats: {stats.AvailablePorts} available, {stats.AllocatedPorts} allocated", ConsoleColor.Green);
            
            // Test discovery service
            var discoveryResult = await _discovery.DiscoverPortsAsync();
            _consoleHelper.DisplayStatus($"🔍 Discovery found {discoveryResult.Count} ports", ConsoleColor.Green);
            
            // Test device analyzer
            var deviceGroups = await _deviceAnalyzer.AnalyzeDeviceGroupsAsync(discoveryResult);
            _consoleHelper.DisplayStatus($"🔀 Device grouping found {deviceGroups.Count} device groups", ConsoleColor.Green);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Foundation services test failed");
            throw;
        }
    }

    /// <summary>
    /// Execute simulated 3-phase workflow
    /// </summary>
    private async Task ExecuteSimulated3PhaseWorkflowAsync(WorkflowResult result, string portName)
    {
        // Simulate PowerOn phase
        _consoleHelper.DisplayStatus("🔋 Phase 1: PowerOn (Simulated)", ConsoleColor.Blue);
        await Task.Delay(500); // Simulate work
        
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
                    Command = "INIT_RS232\\r\\n",
                    Response = "READY",
                    Success = true,
                    ResponseTime = TimeSpan.FromMilliseconds(150)
                }
            }
        };
        
        _consoleHelper.DisplaySuccess("✅ PowerOn phase completed");
        
        // Simulate Test phase
        _consoleHelper.DisplayStatus("🧪 Phase 2: Test (Simulated)", ConsoleColor.Blue);
        await Task.Delay(800); // Simulate work
        
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
                    Command = "RUN_TEST_1\\r\\n",
                    Response = "PASS",
                    Success = true,
                    ResponseTime = TimeSpan.FromMilliseconds(200)
                }
            }
        };
        
        _consoleHelper.DisplaySuccess("✅ Test phase completed");
        
        // Simulate PowerOff phase
        _consoleHelper.DisplayStatus("🔌 Phase 3: PowerOff (Simulated)", ConsoleColor.Blue);
        await Task.Delay(300); // Simulate work
        
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
                    Command = "STOP_RS232\\r\\n",
                    Response = "BYE",
                    Success = true,
                    ResponseTime = TimeSpan.FromMilliseconds(100)
                }
            }
        };
        
        _consoleHelper.DisplaySuccess("✅ PowerOff phase completed");
        
        // Calculate performance metrics
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

    /// <summary>
    /// Discover FTDI devices
    /// </summary>
    private async Task<List<FtdiDeviceInfo>> DiscoverFtdiDevicesAsync()
    {
        var devices = new List<FtdiDeviceInfo>();
        
        try
        {
            var discoveryResult = await _discovery.DiscoverPortsAsync();
            var deviceGroups = await _deviceAnalyzer.AnalyzeDeviceGroupsAsync(discoveryResult);
            
            foreach (var group in deviceGroups)
            {
                if (group.DeviceId.Contains("VID_0403")) // FTDI devices
                {
                    devices.Add(new FtdiDeviceInfo
                    {
                        Description = group.Description,
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

    /// <summary>
    /// Display discovered FTDI devices
    /// </summary>
    private void DisplayDiscoveredDevices(List<FtdiDeviceInfo> devices)
    {
        _consoleHelper.DisplaySuccess($"Found {devices.Count} FTDI device(s):");
        Console.WriteLine();
        
        foreach (var device in devices)
        {
            var icon = device.IsMultiPort ? "🔀" : "📌";
            var portCount = device.IsMultiPort ? $"({device.Ports.Count} ports)" : "(1 port)";
            
            Console.WriteLine($"   {icon} {device.Description} {portCount}");
            Console.WriteLine($"     Device ID: {device.DeviceId}");
            Console.WriteLine($"     Serial: {device.SerialNumber}");
            
            if (device.Ports.Any())
            {
                Console.WriteLine($"     Ports: {string.Join(", ", device.Ports)}");
            }
            
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Test device grouping functionality
    /// </summary>
    private async Task TestDeviceGroupingAsync(List<FtdiDeviceInfo> devices)
    {
        var multiPortDevices = devices.Where(d => d.IsMultiPort).ToList();
        
        if (multiPortDevices.Any())
        {
            _consoleHelper.DisplaySuccess($"✅ Multi-port device grouping working: {multiPortDevices.Count} multi-port device(s)");
            
            foreach (var device in multiPortDevices)
            {
                Console.WriteLine($"   🔀 {device.Description}: {string.Join(", ", device.Ports)}");
            }
        }
        else
        {
            _consoleHelper.DisplayStatus("📌 No multi-port devices found (single-port devices only)", ConsoleColor.Yellow);
        }
        
        await Task.Delay(100);
    }

    /// <summary>
    /// Test real hardware allocation
    /// </summary>
    private async Task TestRealHardwareAllocationAsync(List<FtdiDeviceInfo> devices)
    {
        if (!devices.Any())
        {
            _consoleHelper.DisplayWarning("No FTDI devices available for allocation test");
            return;
        }
        
        try
        {
            // Test allocation of first available device port
            var firstDevice = devices.First();
            var firstPort = firstDevice.Ports.FirstOrDefault();
            
            if (!string.IsNullOrEmpty(firstPort))
            {
                _consoleHelper.DisplayStatus($"🔒 Testing allocation of {firstPort}...", ConsoleColor.Yellow);
                
                var allocation = await _serialPortPool.AllocatePortAsync(clientId: "RealHardwareDemo");
                
                if (allocation != null)
                {
                    _consoleHelper.DisplaySuccess($"✅ Port allocated: {allocation.PortName}");
                    
                    // Release immediately
                    await Task.Delay(1000);
                    var released = await _serialPortPool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
                    
                    if (released)
                    {
                        _consoleHelper.DisplaySuccess($"✅ Port released: {allocation.PortName}");
                    }
                    else
                    {
                        _consoleHelper.DisplayWarning($"⚠️ Port release failed: {allocation.PortName}");
                    }
                }
                else
                {
                    _consoleHelper.DisplayWarning("⚠️ Port allocation failed - no suitable ports available");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Real hardware allocation test failed");
            _consoleHelper.DisplayError($"Allocation test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get user port selection
    /// </summary>
    private string GetUserPortSelection(string[] availablePorts)
    {
        Console.WriteLine("📡 Available serial ports:");
        for (int i = 0; i < availablePorts.Length; i++)
        {
            Console.WriteLine($"   {i + 1}. {availablePorts[i]}");
        }
        Console.WriteLine();
        
        var selection = _consoleHelper.GetUserInput("Select port number (1-" + availablePorts.Length + ", or 'c' to cancel)");
        
        if (selection.ToLowerInvariant() == "c")
            return string.Empty;
            
        if (int.TryParse(selection, out var index) && index >= 1 && index <= availablePorts.Length)
        {
            return availablePorts[index - 1];
        }
        
        _consoleHelper.DisplayWarning("Invalid selection");
        return string.Empty;
    }

    /// <summary>
    /// Get user baud rate selection
    /// </summary>
    private int GetUserBaudRateSelection()
    {
        var baudRates = new[] { 9600, 19200, 38400, 57600, 115200 };
        
        Console.WriteLine("⚡ Available baud rates:");
        for (int i = 0; i < baudRates.Length; i++)
        {
            Console.WriteLine($"   {i + 1}. {baudRates[i]}");
        }
        Console.WriteLine();
        
        var selection = _consoleHelper.GetUserInput("Select baud rate (1-" + baudRates.Length + ")", "5");
        
        if (int.TryParse(selection, out var index) && index >= 1 && index <= baudRates.Length)
        {
            return baudRates[index - 1];
        }
        
        return 115200; // Default
    }

    /// <summary>
    /// Get user timeout selection
    /// </summary>
    private int GetUserTimeoutSelection()
    {
        var timeout = _consoleHelper.GetUserInput("Enter timeout in milliseconds", "5000");
        
        if (int.TryParse(timeout, out var timeoutMs) && timeoutMs > 0 && timeoutMs <= 60000)
        {
            return timeoutMs;
        }
        
        return 5000; // Default
    }

    /// <summary>
    /// Display custom configuration summary
    /// </summary>
    private void DisplayCustomConfiguration(string port, int baudRate, int timeout)
    {
        Console.WriteLine("🔧 Custom Configuration Summary:");
        Console.WriteLine($"   📍 Port: {port}");
        Console.WriteLine($"   ⚡ Baud Rate: {baudRate}");
        Console.WriteLine($"   ⏱️ Timeout: {timeout}ms");
        Console.WriteLine();
    }

    /// <summary>
    /// Test custom configuration
    /// </summary>
    private async Task TestCustomConfigurationAsync(string port, int baudRate, int timeout)
    {
        try
        {
            // Test basic port accessibility
            using var serialPort = new SerialPort(port, baudRate);
            serialPort.ReadTimeout = timeout;
            serialPort.WriteTimeout = timeout;
            
            _consoleHelper.DisplayStatus($"🔌 Opening {port} at {baudRate} baud...", ConsoleColor.Yellow);
            serialPort.Open();
            
            _consoleHelper.DisplaySuccess($"✅ Port opened successfully");
            
            await Task.Delay(1000);
            
            serialPort.Close();
            _consoleHelper.DisplaySuccess($"✅ Port closed successfully");
        }
        catch (Exception ex)
        {
            _consoleHelper.DisplayError($"Custom configuration test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test pool allocation performance
    /// </summary>
    private async Task TestPoolAllocationPerformanceAsync(int iterations)
    {
        var times = new List<double>();
        
        for (int i = 0; i < iterations; i++)
        {
            _consoleHelper.DisplayProgress("Pool allocation test", i + 1, iterations);
            
            var stopwatch = Stopwatch.StartNew();
            var allocation = await _serialPortPool.AllocatePortAsync(clientId: $"PerfTest_{i}");
            stopwatch.Stop();
            
            if (allocation != null)
            {
                times.Add(stopwatch.Elapsed.TotalMilliseconds);
                await _serialPortPool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
            }
            
            await Task.Delay(10); // Small delay between iterations
        }
        
        if (times.Any())
        {
            _performanceMetrics["pool_allocation"] = times;
            var avgTime = times.Average();
            _consoleHelper.DisplaySuccess($"✅ Pool allocation average: {avgTime:F2}ms ({times.Count} successful)");
        }
    }

    /// <summary>
    /// Test discovery performance
    /// </summary>
    private async Task TestDiscoveryPerformanceAsync(int iterations)
    {
        var times = new List<double>();
        
        for (int i = 0; i < iterations; i++)
        {
            _consoleHelper.DisplayProgress("Discovery performance test", i + 1, iterations);
            
            var stopwatch = Stopwatch.StartNew();
            var result = await _discovery.DiscoverPortsAsync();
            stopwatch.Stop();
            
            times.Add(stopwatch.Elapsed.TotalMilliseconds);
            await Task.Delay(50); // Delay between discoveries
        }
        
        _performanceMetrics["discovery"] = times;
        var avgTime = times.Average();
        _consoleHelper.DisplaySuccess($"✅ Discovery average: {avgTime:F2}ms ({iterations} iterations)");
    }

    /// <summary>
    /// Test concurrent operations
    /// </summary>
    private async Task TestConcurrentOperationsAsync()
    {
        _consoleHelper.DisplayStatus("🔀 Testing concurrent operations...", ConsoleColor.Yellow);
        
        var concurrentTasks = new List<Task>();
        var maxConcurrent = _configuration.GetValue<int>("Demo:PerformanceTesting:MaxConcurrentConnections");
        
        for (int i = 0; i < maxConcurrent; i++)
        {
            var task = TestConcurrentAllocationAsync($"Concurrent_{i}");
            concurrentTasks.Add(task);
        }
        
        await Task.WhenAll(concurrentTasks);
        _consoleHelper.DisplaySuccess($"✅ Concurrent operations completed ({maxConcurrent} concurrent)");
    }

    /// <summary>
    /// Test concurrent allocation
    /// </summary>
    private async Task TestConcurrentAllocationAsync(string clientId)
    {
        try
        {
            var allocation = await _serialPortPool.AllocatePortAsync(clientId: clientId);
            if (allocation != null)
            {
                await Task.Delay(100); // Hold allocation briefly
                await _serialPortPool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Concurrent allocation failed for {ClientId}: {Error}", clientId, ex.Message);
        }
    }

    /// <summary>
    /// Display performance metrics
    /// </summary>
    private void DisplayPerformanceMetrics()
    {
        Console.WriteLine();
        _consoleHelper.DisplayStatus("📊 Performance Metrics Summary:", ConsoleColor.Cyan);
        
        foreach (var metric in _performanceMetrics)
        {
            var times = metric.Value;
            if (times.Any())
            {
                var avg = times.Average();
                var min = times.Min();
                var max = times.Max();
                
                Console.WriteLine($"   {metric.Key}: avg={avg:F2}ms, min={min:F2}ms, max={max:F2}ms ({times.Count} samples)");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"📈 Total workflows executed: {_totalWorkflowsExecuted}");
        Console.WriteLine($"✅ Successful workflows: {_successfulWorkflows}");
        
        if (_totalWorkflowsExecuted > 0)
        {
            var successRate = (double)_successfulWorkflows / _totalWorkflowsExecuted * 100;
            Console.WriteLine($"📊 Success rate: {successRate:F1}%");
        }
    }

    /// <summary>
    /// Collect comprehensive system information
    /// </summary>
    private async Task<SystemInfo> CollectSystemInformationAsync()
    {
        var systemInfo = new SystemInfo
        {
            ApplicationUptime = _applicationStopwatch.Elapsed,
            MemoryUsageMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            ThreadCount = Process.GetCurrentProcess().Threads.Count,
            FoundationServicesCount = 5, // Foundation services from Sprint 3-4
            Sprint5ServicesReady = false, // Sprint 5 services pending
            ConfigurationFilesCount = 1
        };
        
        // Collect port information
        var availablePorts = SerialPort.GetPortNames();
        foreach (var portName in availablePorts)
        {
            systemInfo.AvailablePorts.Add(new PortInfo
            {
                PortName = portName,
                Description = $"Serial port {portName}",
                Manufacturer = "Unknown"
            });
        }
        
        // Collect FTDI device information
        try
        {
            var ftdiDevices = await DiscoverFtdiDevicesAsync();
            systemInfo.FtdiDevices.AddRange(ftdiDevices);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to collect FTDI device information: {Error}", ex.Message);
        }
        
        return systemInfo;
    }
}