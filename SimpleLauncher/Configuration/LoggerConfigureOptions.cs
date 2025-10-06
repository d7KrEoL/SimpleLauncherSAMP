using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleLauncher.Services;
using System;
using System.Collections.Generic;
using System.Text;

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
