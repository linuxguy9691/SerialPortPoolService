using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // ← Ajoutez cette ligne
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

public class FtdiDeviceReaderTests
{
    private readonly FtdiDeviceReader _reader;

    public FtdiDeviceReaderTests()
    {
        // Version simplifiée sans LoggerFactory
        var logger = NullLogger<FtdiDeviceReader>.Instance;
        _reader = new FtdiDeviceReader(logger);
    }
    
    // ... rest of the tests
}

public class SerialPortValidatorTests
{
    private readonly SerialPortValidator _validator;
    private readonly FtdiDeviceReader _ftdiReader;

    public SerialPortValidatorTests()
    {
        // Version simplifiée
        var ftdiLogger = NullLogger<FtdiDeviceReader>.Instance;
        var validatorLogger = NullLogger<SerialPortValidator>.Instance;
        
        _ftdiReader = new FtdiDeviceReader(ftdiLogger);
        _validator = new SerialPortValidator(_ftdiReader, validatorLogger);
    }
    
    // ... rest of the tests
}