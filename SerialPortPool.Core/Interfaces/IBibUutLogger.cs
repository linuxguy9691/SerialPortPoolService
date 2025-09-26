using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Interfaces
{
    public interface IBibUutLogger
    {
        void LogBibExecution(LogLevel level, string message, params object[] args);
    }
}