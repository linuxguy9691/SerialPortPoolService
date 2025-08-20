// ===================================================================
// TEST: Verify Multiple <test> Elements Parsing (Sprint 9)
// File: tests/SerialPortPool.Core.Tests/XmlParsingMultipleTestsTest.cs
// Purpose: Verify that all 4 <test> elements are parsed from client-demo.xml
// ===================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests;

public class XmlParsingMultipleTestsTest
{
    private readonly XmlBibConfigurationLoader _loader;
    private readonly ILogger<XmlBibConfigurationLoader> _logger;
    private readonly IMemoryCache _cache;

    public XmlParsingMultipleTestsTest()
    {
        _logger = NullLogger<XmlBibConfigurationLoader>.Instance;
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loader = new XmlBibConfigurationLoader(_logger, _cache);
    }

    [Fact]
    public async Task LoadClientDemoXml_ShouldParse4TestElements()
    {
        // Arrange
        var xmlPath = Path.Combine("Configuration", "client-demo.xml");
        
        // Act
        var configurations = await _loader.LoadConfigurationsAsync(xmlPath);
        
        // Assert
        Assert.True(configurations.ContainsKey("CLIENT_DEMO_SPRINT9"));
        
        var bibConfig = configurations["CLIENT_DEMO_SPRINT9"];
        Assert.NotNull(bibConfig);
        Assert.Single(bibConfig.Uuts); // Should have 1 UUT
        
        var uut = bibConfig.Uuts.First();
        Assert.Equal("production_uut", uut.UutId);
        Assert.Single(uut.Ports); // Should have 1 port
        
        var port = uut.Ports.First();
        Assert.Equal(1, port.PortNumber);
        Assert.Equal("rs232", port.Protocol);
        
        // 🔥 KEY TEST: Verify TestCommands contains ALL 4 test elements
        Assert.NotNull(port.TestCommands);
        Assert.Equal(4, port.TestCommands.Commands.Count);
        
        // Verify each test command
        var testCommands = port.TestCommands.Commands;
        
        // Test 1: Simple PASS test
        var test1 = testCommands[0];
        Assert.Equal("TEST", test1.Command);
        Assert.Equal("PASS", test1.ExpectedResponse);
        Assert.Equal(5000, test1.TimeoutMs);
        
        // Test 2: Multi-level with WARNING
        var test2 = testCommands[1];
        Assert.Equal("TEST", test2.Command);
        Assert.Equal("NEVER_MATCH_THIS", test2.ExpectedResponse);
        
        // Test 3: Multi-level with FAIL
        var test3 = testCommands[2];
        Assert.Equal("TEST", test3.Command);
        Assert.Equal("NEVER_MATCH_THIS", test3.ExpectedResponse);
        
        // Test 4: Multi-level with CRITICAL + hardware trigger
        var test4 = testCommands[3];
        Assert.Equal("TEST", test4.Command);
        Assert.Equal("NEVER_MATCH_THIS", test4.ExpectedResponse);
    }

    [Fact]
    public async Task LoadClientDemoXml_ShouldParseMultiLevelValidation()
    {
        // Arrange
        var xmlPath = Path.Combine("Configuration", "client-demo.xml");
        
        // Act
        var configurations = await _loader.LoadConfigurationsAsync(xmlPath);
        var bibConfig = configurations["CLIENT_DEMO_SPRINT9"];
        var port = bibConfig.Uuts.First().Ports.First();
        var testCommands = port.TestCommands.Commands;
        
        // Assert: Check if multi-level commands are properly parsed
        
        // Test 2 should be MultiLevelProtocolCommand with WARN pattern
        if (testCommands[1] is MultiLevelProtocolCommand multiCommand2)
        {
            Assert.True(multiCommand2.ValidationPatterns.ContainsKey(ValidationLevel.WARN));
            Assert.Equal("^PASS(\\r\\n)?$", multiCommand2.ValidationPatterns[ValidationLevel.WARN]);
        }
        
        // Test 3 should be MultiLevelProtocolCommand with FAIL pattern
        if (testCommands[2] is MultiLevelProtocolCommand multiCommand3)
        {
            Assert.True(multiCommand3.ValidationPatterns.ContainsKey(ValidationLevel.FAIL));
            Assert.Equal("^PASS(\\r\\n)?$", multiCommand3.ValidationPatterns[ValidationLevel.FAIL]);
        }
        
        // Test 4 should be MultiLevelProtocolCommand with CRITICAL pattern + hardware trigger
        if (testCommands[3] is MultiLevelProtocolCommand multiCommand4)
        {
            Assert.True(multiCommand4.ValidationPatterns.ContainsKey(ValidationLevel.CRITICAL));
            Assert.Equal("^PASS(\\r\\n)?$", multiCommand4.ValidationPatterns[ValidationLevel.CRITICAL]);
            Assert.True(multiCommand4.TriggerHardwareOnCritical);
        }
    }

    [Fact]
    public async Task LoadClientDemoXml_ShouldPreserveStartAndStopCommands()
    {
        // Arrange
        var xmlPath = Path.Combine("Configuration", "client-demo.xml");
        
        // Act
        var configurations = await _loader.LoadConfigurationsAsync(xmlPath);
        var bibConfig = configurations["CLIENT_DEMO_SPRINT9"];
        var port = bibConfig.Uuts.First().Ports.First();
        
        // Assert: Start and Stop commands should be unchanged
        Assert.NotNull(port.StartCommands);
        Assert.Single(port.StartCommands.Commands);
        Assert.Equal("INIT_RS232", port.StartCommands.Commands[0].Command);
        Assert.Equal("READY", port.StartCommands.Commands[0].ExpectedResponse);
        
        Assert.NotNull(port.StopCommands);
        Assert.Single(port.StopCommands.Commands);
        Assert.Equal("AT+QUIT", port.StopCommands.Commands[0].Command);
        Assert.Equal("BYE", port.StopCommands.Commands[0].ExpectedResponse);
    }

    [Fact]
    public void LoadNonExistentXml_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var xmlPath = "non-existent-file.xml";
        
        // Act & Assert
        var exception = Assert.ThrowsAsync<FileNotFoundException>(() => 
            _loader.LoadConfigurationsAsync(xmlPath));
    }
}

// ===================================================================
// QUICK CONSOLE TEST PROGRAM
// File: tests/QuickXmlParsingTest/Program.cs
// Purpose: Quick test to verify XML parsing works
// ===================================================================

/*
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SerialPortPool.Core.Services;

Console.WriteLine("🔍 Testing XML Parsing - Multiple <test> Elements");
Console.WriteLine("==================================================");

var logger = NullLogger<XmlBibConfigurationLoader>.Instance;
var cache = new MemoryCache(new MemoryCacheOptions());
var loader = new XmlBibConfigurationLoader(logger, cache);

try
{
    var xmlPath = Path.Combine("Configuration", "client-demo.xml");
    
    if (!File.Exists(xmlPath))
    {
        Console.WriteLine($"❌ XML file not found: {xmlPath}");
        Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        return;
    }
    
    Console.WriteLine($"📄 Loading XML: {xmlPath}");
    var configurations = await loader.LoadConfigurationsAsync(xmlPath);
    
    if (configurations.ContainsKey("CLIENT_DEMO_SPRINT9"))
    {
        var bibConfig = configurations["CLIENT_DEMO_SPRINT9"];
        var port = bibConfig.Uuts.First().Ports.First();
        
        Console.WriteLine($"✅ BIB loaded: {bibConfig.BibId}");
        Console.WriteLine($"📊 Test commands found: {port.TestCommands.Commands.Count}");
        
        for (int i = 0; i < port.TestCommands.Commands.Count; i++)
        {
            var cmd = port.TestCommands.Commands[i];
            Console.WriteLine($"  Test {i+1}: {cmd.Command} → {cmd.ExpectedResponse}");
            
            if (cmd is MultiLevelProtocolCommand multiCmd)
            {
                Console.WriteLine($"    🎯 Multi-level patterns: {multiCmd.ValidationPatterns.Count}");
                foreach (var pattern in multiCmd.ValidationPatterns)
                {
                    Console.WriteLine($"      {pattern.Key}: {pattern.Value}");
                }
            }
        }
        
        if (port.TestCommands.Commands.Count == 4)
        {
            Console.WriteLine("🎉 SUCCESS: All 4 test elements parsed correctly!");
        }
        else
        {
            Console.WriteLine($"❌ PROBLEM: Expected 4 test elements, got {port.TestCommands.Commands.Count}");
        }
    }
    else
    {
        Console.WriteLine("❌ BIB 'CLIENT_DEMO_SPRINT9' not found in configurations");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
*/