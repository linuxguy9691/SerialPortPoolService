using System.Text;

namespace SerialPortPool.Demo;

/// <summary>
/// Console Helper - Rich formatting utilities for spectacular demo output
/// Provides colored output, progress indicators, tables, and interactive elements
/// </summary>
public class ConsoleHelper
{
    private readonly object _lockObject = new object();
    
    /// <summary>
    /// Display comprehensive help and documentation
    /// </summary>
    public void DisplayHelpInformation()
    {
        lock (_lockObject)
        {
            Console.Clear();
            DisplayHeader("❓ RS232Demo Help & Documentation", ConsoleColor.Cyan);
            
            Console.WriteLine();
            DisplaySection("📋 Quick Start Guide", ConsoleColor.Green);
            Console.WriteLine("   1. 🤖 For safe testing: Start Python dummy UUT first");
            Console.WriteLine("      python ../DummyUUT/dummy_uut.py --port COM8");
            Console.WriteLine("   2. 🎬 Launch this demo application");
            Console.WriteLine("      dotnet run");
            Console.WriteLine("   3. 📋 Select scenario 1 (Python Dummy UUT)");
            Console.WriteLine("   4. 🚀 Watch the workflow execute");
            Console.WriteLine("   5. ✅ See complete 3-phase results");
            Console.WriteLine();
            
            DisplaySection("🎯 Demo Scenarios Explained", ConsoleColor.Yellow);
            Console.WriteLine("   🤖 Scenario 1: Python Dummy UUT");
            Console.WriteLine("      • Safe testing with simulated device responses");
            Console.WriteLine("      • No hardware required, predictable results");
            Console.WriteLine("      • Perfect for development and demonstrations");
            Console.WriteLine();
            Console.WriteLine("   🏭 Scenario 2: Real Hardware Detection");
            Console.WriteLine("      • Automatic FT4232H device discovery");
            Console.WriteLine("      • Uses actual industrial hardware");
            Console.WriteLine("      • Validates real-world compatibility");
            Console.WriteLine();
            Console.WriteLine("   🔧 Scenario 3: Custom Configuration");
            Console.WriteLine("      • Manual port and parameter selection");
            Console.WriteLine("      • Custom XML configuration loading");
            Console.WriteLine("      • Advanced user scenarios");
            Console.WriteLine();
            Console.WriteLine("   📊 Scenario 4: Performance Testing");
            Console.WriteLine("      • Stress testing and benchmarking");
            Console.WriteLine("      • Multiple concurrent workflows");
            Console.WriteLine("      • Performance metrics and analysis");
            Console.WriteLine();
            
            DisplaySection("🔧 Troubleshooting", ConsoleColor.Red);
            Console.WriteLine("   ❌ \"Port COM8 not available\"");
            Console.WriteLine("      → Check if Python dummy UUT is running");
            Console.WriteLine("      → Try different port: python dummy_uut.py --port COM9");
            Console.WriteLine("      → Check available ports in Scenario 5");
            Console.WriteLine();
            Console.WriteLine("   ❌ \"Build errors\"");
            Console.WriteLine("      → Ensure SerialPortPool.Core is built first");
            Console.WriteLine("      → Run: dotnet build ../../SerialPortPool.Core/");
            Console.WriteLine("      → Check references in .csproj file");
            Console.WriteLine();
            Console.WriteLine("   ❌ \"Service registration failed\"");
            Console.WriteLine("      → Check that all dependencies are available");
            Console.WriteLine("      → Review logs for specific service errors");
            Console.WriteLine("      → Ensure .NET 9.0 is installed");
            Console.WriteLine();
            
            DisplaySection("📚 Documentation Links", ConsoleColor.Magenta);
            Console.WriteLine("   📖 Complete Demo Guide: README.md (this directory)");
            Console.WriteLine("   🏗️ Sprint 5 Architecture: ../../docs/sprint5/");
            Console.WriteLine("   🤖 Dummy UUT Setup: ../DummyUUT/README.md");
            Console.WriteLine("   🔧 SerialPortPool Core: ../../SerialPortPool.Core/");
            Console.WriteLine();
            
            DisplaySection("⚡ Pro Tips", ConsoleColor.Blue);
            Console.WriteLine("   💡 Use Scenario 5 to check available ports before testing");
            Console.WriteLine("   💡 Keep Python dummy UUT running in separate terminal");
            Console.WriteLine("   💡 Check logs in C:\\Logs\\SerialPortPool\\ for detailed info");
            Console.WriteLine("   💡 Use Ctrl+C to interrupt long-running operations");
            Console.WriteLine();
            
            DisplayFooter("Press any key to return to main menu...");
        }
    }
    
    /// <summary>
    /// Display workflow execution results with rich formatting
    /// </summary>
    public void DisplayWorkflowResult(WorkflowResult result)
    {
        lock (_lockObject)
        {
            Console.WriteLine();
            DisplayHeader("📊 BIB Workflow Execution Results", ConsoleColor.Cyan);
            
            // Basic information
            Console.WriteLine($"   🆔 Workflow ID: {result.WorkflowId}");
            Console.WriteLine($"   📍 Configuration: {result.BibId}.{result.UutId}.Port{result.PortNumber}");
            Console.WriteLine($"   📡 Protocol: {result.Protocol}");
            Console.WriteLine($"   🔌 Physical Port: {result.PortName}");
            Console.WriteLine($"   ⏱️  Duration: {result.Duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"   🕒 Started: {result.StartTime:HH:mm:ss}");
            Console.WriteLine($"   🏁 Completed: {result.EndTime:HH:mm:ss}");
            Console.WriteLine();
            
            // Phase results
            DisplayPhaseResults(result);
            
            // Overall status
            DisplayOverallStatus(result);
            
            // Performance metrics
            if (result.PerformanceMetrics != null)
            {
                DisplayPerformanceMetrics(result.PerformanceMetrics);
            }
            
            // Error details if any
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                DisplayErrorDetails(result.ErrorMessage);
            }
        }
    }
    
    /// <summary>
    /// Display 3-phase workflow results
    /// </summary>
    private void DisplayPhaseResults(WorkflowResult result)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🚀 3-Phase Workflow Execution:");
        Console.ForegroundColor = ConsoleColor.White;
        
        DisplayPhaseResult("🔋 PowerOn Phase", result.PowerOnResult, 1);
        DisplayPhaseResult("🧪 Test Phase", result.TestResult, 2);
        DisplayPhaseResult("🔌 PowerOff Phase", result.PowerOffResult, 3);
    }
    
    /// <summary>
    /// Display individual phase result
    /// </summary>
    private void DisplayPhaseResult(string phaseName, PhaseResult? phase, int phaseNumber)
    {
        if (phase == null)
        {
            Console.WriteLine($"   {phaseName}: ⏭️  Skipped");
            return;
        }
        
        var success = phase.Success;
        var statusIcon = success ? "✅" : "❌";
        var commandSummary = $"{phase.SuccessfulCommands}/{phase.TotalCommands}";
        
        Console.WriteLine($"   {phaseName}: {statusIcon} {commandSummary} commands successful");
        
        // Show command details
        if (phase.Commands.Any())
        {
            foreach (var cmd in phase.Commands)
            {
                var cmdIcon = cmd.Success ? "  ✓" : "  ✗";
                var cmdStatus = cmd.Success ? ConsoleColor.Green : ConsoleColor.Red;
                
                Console.ForegroundColor = cmdStatus;
                Console.WriteLine($"{cmdIcon} {cmd.Command.Trim()} → {cmd.Response?.Trim() ?? "NO_RESPONSE"}");
                Console.ForegroundColor = ConsoleColor.White;
                
                if (!cmd.Success && !string.IsNullOrEmpty(cmd.ErrorMessage))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"      Error: {cmd.ErrorMessage}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
        
        if (!success)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"      ⚠️  Phase {phaseNumber} failed - workflow may be incomplete");
            Console.ForegroundColor = ConsoleColor.White;
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// Display overall workflow status
    /// </summary>
    private void DisplayOverallStatus(WorkflowResult result)
    {
        var statusColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
        var statusText = result.Success ? "✅ SUCCESS" : "❌ FAILED";
        var completionRate = result.CalculateCompletionRate();
        
        Console.ForegroundColor = statusColor;
        Console.WriteLine($"📋 Overall Result: {statusText}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"📊 Completion Rate: {completionRate:F1}%");
        Console.WriteLine($"📈 Commands Executed: {result.GetTotalCommandsExecuted()}/{result.GetTotalCommandsPlanned()}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Display performance metrics
    /// </summary>
    private void DisplayPerformanceMetrics(PerformanceMetrics metrics)
    {
        DisplaySection("⚡ Performance Metrics", ConsoleColor.Blue);
        
        Console.WriteLine($"   📊 Average Command Response Time: {metrics.AverageResponseTime:F0}ms");
        Console.WriteLine($"   📊 Fastest Command: {metrics.FastestCommand:F0}ms");
        Console.WriteLine($"   📊 Slowest Command: {metrics.SlowestCommand:F0}ms");
        Console.WriteLine($"   📊 Total Communication Time: {metrics.TotalCommunicationTime:F0}ms");
        Console.WriteLine($"   📊 Overhead Time: {metrics.OverheadTime:F0}ms");
        
        if (metrics.ThroughputCommandsPerSecond > 0)
        {
            Console.WriteLine($"   📊 Throughput: {metrics.ThroughputCommandsPerSecond:F1} commands/second");
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// Display error details
    /// </summary>
    private void DisplayErrorDetails(string errorMessage)
    {
        DisplaySection("❌ Error Details", ConsoleColor.Red);
        Console.WriteLine($"   {errorMessage}");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("💡 Troubleshooting suggestions:");
        Console.WriteLine("   • Check that the target device is connected and powered");
        Console.WriteLine("   • Verify that the Python dummy UUT is running (for demo scenarios)");
        Console.WriteLine("   • Ensure the correct port is specified in configuration");
        Console.WriteLine("   • Check that no other application is using the serial port");
        Console.WriteLine("   • Review detailed logs for more information");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
    }
    
    /// <summary>
    /// Display progress indicator
    /// </summary>
    public void DisplayProgress(string operation, int current, int total)
    {
        lock (_lockObject)
        {
            var percentage = (double)current / total * 100;
            var progressBar = CreateProgressBar(percentage, 30);
            
            Console.Write($"\r🔄 {operation}: [{progressBar}] {percentage:F0}% ({current}/{total})");
            
            if (current == total)
            {
                Console.WriteLine(); // New line when complete
            }
        }
    }
    
    /// <summary>
    /// Display status message with timestamp
    /// </summary>
    public void DisplayStatus(string message, ConsoleColor color = ConsoleColor.White)
    {
        lock (_lockObject)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{timestamp}] ");
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
    
    /// <summary>
    /// Display system information table
    /// </summary>
    public void DisplaySystemInformation(SystemInfo systemInfo)
    {
        lock (_lockObject)
        {
            Console.Clear();
            DisplayHeader("🔍 System Information", ConsoleColor.Cyan);
            
            // Serial Ports
            DisplaySection("📡 Available Serial Ports", ConsoleColor.Green);
            if (systemInfo.AvailablePorts.Any())
            {
                foreach (var port in systemInfo.AvailablePorts)
                {
                    Console.WriteLine($"   • {port.PortName} - {port.Description}");
                    if (!string.IsNullOrEmpty(port.Manufacturer))
                    {
                        Console.WriteLine($"     Manufacturer: {port.Manufacturer}");
                    }
                }
            }
            else
            {
                Console.WriteLine("   ❌ No serial ports detected");
            }
            Console.WriteLine();
            
            // FTDI Devices
            if (systemInfo.FtdiDevices.Any())
            {
                DisplaySection("🏭 FTDI Devices", ConsoleColor.Blue);
                foreach (var device in systemInfo.FtdiDevices)
                {
                    var icon = device.IsMultiPort ? "🔀" : "📌";
                    Console.WriteLine($"   {icon} {device.Description}");
                    Console.WriteLine($"     Device ID: {device.DeviceId}");
                    Console.WriteLine($"     Serial Number: {device.SerialNumber}");
                    if (device.Ports.Any())
                    {
                        Console.WriteLine($"     Ports: {string.Join(", ", device.Ports)}");
                    }
                }
                Console.WriteLine();
            }
            
            // Service Status
            DisplaySection("🔧 Service Status", ConsoleColor.Yellow);
            Console.WriteLine($"   Foundation Services: ✅ {systemInfo.FoundationServicesCount} loaded");
            Console.WriteLine($"   Sprint 5 Services: {(systemInfo.Sprint5ServicesReady ? "✅" : "⏳")} {(systemInfo.Sprint5ServicesReady ? "Ready" : "Pending")}");
            Console.WriteLine($"   Configuration Files: ✅ {systemInfo.ConfigurationFilesCount} available");
            Console.WriteLine();
            
            // Performance Info
            DisplaySection("📊 Performance Information", ConsoleColor.Magenta);
            Console.WriteLine($"   Application Uptime: {systemInfo.ApplicationUptime.TotalSeconds:F1} seconds");
            Console.WriteLine($"   Memory Usage: {systemInfo.MemoryUsageMB:F1} MB");
            Console.WriteLine($"   Thread Count: {systemInfo.ThreadCount}");
            Console.WriteLine();
            
            DisplayFooter("Press any key to return to main menu...");
        }
    }
    
    /// <summary>
    /// Create a progress bar string
    /// </summary>
    private string CreateProgressBar(double percentage, int width)
    {
        var filled = (int)(percentage / 100 * width);
        var empty = width - filled;
        
        return new string('█', filled) + new string('░', empty);
    }
    
    /// <summary>
    /// Display a section header
    /// </summary>
    private void DisplaySection(string title, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(title);
        Console.ForegroundColor = ConsoleColor.White;
    }
    
    /// <summary>
    /// Display a main header with underline
    /// </summary>
    private void DisplayHeader(string title, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.ForegroundColor = ConsoleColor.White;
    }
    
    /// <summary>
    /// Display footer message
    /// </summary>
    private void DisplayFooter(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
    }
    
    /// <summary>
    /// Get user confirmation
    /// </summary>
    public bool GetUserConfirmation(string message, bool defaultValue = true)
    {
        var defaultText = defaultValue ? "[Y/n]" : "[y/N]";
        Console.Write($"{message} {defaultText}: ");
        
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        
        if (string.IsNullOrEmpty(input))
            return defaultValue;
            
        return input.StartsWith("y");
    }
    
    /// <summary>
    /// Get user input with prompt
    /// </summary>
    public string GetUserInput(string prompt, string defaultValue = "")
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            Console.Write($"{prompt} [{defaultValue}]: ");
        }
        else
        {
            Console.Write($"{prompt}: ");
        }
        
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }
    
    /// <summary>
    /// Display warning message
    /// </summary>
    public void DisplayWarning(string message)
    {
        lock (_lockObject)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  WARNING: {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
    
    /// <summary>
    /// Display error message
    /// </summary>
    public void DisplayError(string message)
    {
        lock (_lockObject)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR: {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
    
    /// <summary>
    /// Display success message
    /// </summary>
    public void DisplaySuccess(string message)
    {
        lock (_lockObject)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
    
    /// <summary>
    /// Clear console and return to top
    /// </summary>
    public void ClearScreen()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);
    }
}

// Supporting model classes for rich display
public class WorkflowResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public string BibId { get; set; } = string.Empty;
    public string UutId { get; set; } = string.Empty;
    public int PortNumber { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public string PortName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public string ErrorMessage { get; set; } = string.Empty;
    
    public PhaseResult? PowerOnResult { get; set; }
    public PhaseResult? TestResult { get; set; }
    public PhaseResult? PowerOffResult { get; set; }
    public PerformanceMetrics? PerformanceMetrics { get; set; }
    
    public double CalculateCompletionRate()
    {
        var phases = new[] { PowerOnResult, TestResult, PowerOffResult }.Where(p => p != null).ToList();
        if (!phases.Any()) return 0;
        
        var totalCommands = phases.Sum(p => p!.TotalCommands);
        var successfulCommands = phases.Sum(p => p!.SuccessfulCommands);
        
        return totalCommands > 0 ? (double)successfulCommands / totalCommands * 100 : 0;
    }
    
    public int GetTotalCommandsExecuted()
    {
        return new[] { PowerOnResult, TestResult, PowerOffResult }
            .Where(p => p != null)
            .Sum(p => p!.Commands.Count);
    }
    
    public int GetTotalCommandsPlanned()
    {
        return new[] { PowerOnResult, TestResult, PowerOffResult }
            .Where(p => p != null)
            .Sum(p => p!.TotalCommands);
    }
}

public class PhaseResult
{
    public bool Success { get; set; }
    public int TotalCommands { get; set; }
    public int SuccessfulCommands { get; set; }
    public List<CommandResult> Commands { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class CommandResult
{
    public string Command { get; set; } = string.Empty;
    public string? Response { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

public class PerformanceMetrics
{
    public double AverageResponseTime { get; set; }
    public double FastestCommand { get; set; }
    public double SlowestCommand { get; set; }
    public double TotalCommunicationTime { get; set; }
    public double OverheadTime { get; set; }
    public double ThroughputCommandsPerSecond { get; set; }
}

public class SystemInfo
{
    public List<PortInfo> AvailablePorts { get; set; } = new();
    public List<FtdiDeviceInfo> FtdiDevices { get; set; } = new();
    public int FoundationServicesCount { get; set; }
    public bool Sprint5ServicesReady { get; set; }
    public int ConfigurationFilesCount { get; set; }
    public TimeSpan ApplicationUptime { get; set; }
    public double MemoryUsageMB { get; set; }
    public int ThreadCount { get; set; }
}

public class PortInfo
{
    public string PortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
}

public class FtdiDeviceInfo
{
    public string Description { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsMultiPort { get; set; }
    public List<string> Ports { get; set; } = new();
}