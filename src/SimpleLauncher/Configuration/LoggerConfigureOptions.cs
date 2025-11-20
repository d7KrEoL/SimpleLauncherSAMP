using Microsoft.Extensions.Options;

namespace SimpleLauncher.Configuration
{
    class LoggerConfigureOptions : IConfigureNamedOptions<LoggerConfigureOptions>
    {
        public void Configure(string? name,
            LoggerConfigureOptions loggerConfigureOptions)
        {
            
        }

        public void Configure(LoggerConfigureOptions loggerFactory)
        {
            
        }
    }
}
