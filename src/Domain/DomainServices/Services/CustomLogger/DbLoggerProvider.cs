using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services.CustomLogger
{
    public class DbLoggerProvider: ILoggerProvider
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IConfiguration _configuration;

        public DbLoggerProvider(
            ISecurityContextProvider securityContextProvider,
            IConfiguration configuration

        )
        {
            _securityContextProvider = securityContextProvider;
            _configuration = configuration;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new DbLogger(categoryName, _securityContextProvider, _configuration);
        }

        public void Dispose() { }
    }
}