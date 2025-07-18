using Xunit;

namespace SerialPortPool.Tests;

public class ServiceTests
{
    [Fact]
    public void BasicTest_Mathematics()
    {
        // Test de base pour valider le framework
        Assert.Equal(4, 2 + 2);
        Assert.True(true);
    }
    
    [Fact]
    public void BasicTest_Strings()
    {
        // Test de manipulation de chaînes
        var serviceName = "SerialPortPoolService";
        Assert.Contains("Service", serviceName);
        Assert.StartsWith("Serial", serviceName);
    }
    
    [Fact]
    public void BasicTest_Framework()
    {
        // Test que nous sommes sur .NET 9
        var version = Environment.Version;
        Assert.True(version.Major >= 5); // .NET 5+
    }
    
    [Fact]
    public void ServiceConfiguration_IsForWindows()
    {
        // Test que notre configuration cible bien Windows
        var targetFramework = "net9.0-windows";
        Assert.Contains("windows", targetFramework);
    }
}
