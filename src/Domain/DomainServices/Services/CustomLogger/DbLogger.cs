using System;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CustomLogger;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services.CustomLogger
{
    public class DbLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly bool _isEnabledLogRecord;

        public DbLogger(
            string categoryName,
            ISecurityContextProvider securityContextProvider, 
            IConfiguration configuration
        )
        {
            _categoryName = categoryName;
            _securityContextProvider = securityContextProvider;
            _isEnabledLogRecord = configuration["EnableLogRecord"] == "1";
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true; // Log all

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            try
            {
                if (!(exception != null || logLevel == LogLevel.Error) || eventId.Name == "TokenValidationFailed" || !_isEnabledLogRecord) return;
                var message = formatter?.Invoke(state, exception) ?? state?.ToString() ?? string.Empty;

                var logEntry = new AppLogRecord
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow,
                    LogLevel = logLevel.ToString(),
                    State = state?.ToString(),
                    Source = _categoryName,
                    Message = message,
                    ExceptionMessage = exception?.Message,
                    StackTrace = exception?.StackTrace,
                    EventId = eventId
                };

                if (!string.IsNullOrEmpty(logEntry.State) && logEntry.Source == "Microsoft.AspNetCore.Server.Kestrel" 
                    && logEntry.ExceptionMessage == "Unexpected end of request content.")
                {
                    return;
                }

                PublishLogRecordedEvent(logEntry);
            }
            catch (Exception ex)
            {
                var _logger = ServiceLocator.GetService<ILogger<DbLogger>>();
                _logger.LogWarning("Prevent logger from crashing the app. Exception->{message}", ex.Message);
            }
        }

        private void PublishLogRecordedEvent(AppLogRecord logRecord)
        {
            var logRecordEvent = new GenericEvent
            {
                EventType = PraxisEventType.AppLogRecordedEvent,
                JsonPayload = JsonConvert.SerializeObject(logRecord)
            };

            var _serviceClient = ServiceLocator.GetService<IServiceClient>();
            SecurityContext? securityContext = null;
            try
            {
                securityContext = _securityContextProvider.GetSecurityContext();
            }
            catch 
            { 
                securityContext = PraxisConstants.CreateSecurityContext();
            }
            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), logRecordEvent, securityContext.Value);
        }
    }
}