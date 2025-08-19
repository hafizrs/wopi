using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule
{
    /// <summary>
    /// Background service that runs every day at configurable time to clean up ALL WOPI sessions
    /// and their associated local disk files
    /// </summary>
    public class WopiSessionCleanupService : BackgroundService
    {
        private readonly ILogger<WopiSessionCleanupService> _logger;
        private readonly IConfiguration _configuration;
        private Timer _timer;

        public WopiSessionCleanupService(
            ILogger<WopiSessionCleanupService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WOPI Session Cleanup Service started");
            
            // Schedule the first cleanup
            ScheduleNextCleanup();
            
            return Task.CompletedTask;
        }

        private void ScheduleNextCleanup()
        {
            try
            {
                var cleanupTime = _configuration["WopiCleanupTime"] ?? "00:05";
                
                if (TimeSpan.TryParse(cleanupTime, out var timeSpan))
                {
                    var nextRun = GetNextRunTime(timeSpan);
                    var delay = nextRun - DateTime.UtcNow;
                    
                    _logger.LogInformation("Next cleanup scheduled for: {NextRunTime} (in {Delay})", nextRun, delay);
                    
                    // Dispose existing timer if any
                    _timer?.Dispose();
                    
                    // Create new timer
                    _timer = new Timer(DoCleanup, null, delay, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    _logger.LogWarning("Invalid cleanup time format: {CleanupTime}. Using default: 00:05 UTC", cleanupTime);
                    ScheduleNextCleanup(); // Retry with default
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling cleanup");
            }
        }

        private DateTime GetNextRunTime(TimeSpan cleanupTime)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.Add(cleanupTime);
            
            // If today's time has passed, schedule for tomorrow
            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }
            
            return nextRun;
        }

        private void DoCleanup(object state)
        {
            try
            {
                _logger.LogInformation("Starting WOPI session cleanup at {Time}", DateTime.UtcNow);
                
                PerformCleanupAsync();
                
                // Schedule next cleanup
                ScheduleNextCleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                
                // Retry in 1 hour if there's an error
                _timer?.Change(TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan);
            }
        }

        private void PerformCleanupAsync()
        {
            var startTime = DateTime.UtcNow;
            var totalSessions = 0;
            var deletedFiles = 0;
            var errors = 0;

            try
            {
                // Get all sessions
                var allSessions = WopiSessionStore.GetAll().ToList();
                totalSessions = allSessions.Count;
                
                _logger.LogInformation("Found {TotalSessions} total sessions to delete", totalSessions);

                foreach (var session in allSessions)
                {
                    try
                    {
                        // Delete the session
                         WopiSessionStore.Delete(session.ItemId);
                        
                        deletedFiles++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing session {SessionId} during cleanup", session.SessionId);
                        errors++;
                    }
                }

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("WOPI session cleanup completed in {Duration}. " +
                    "Total sessions: {TotalSessions}, Deleted files: {DeletedFiles}, Errors: {Errors}",
                    duration, totalSessions, deletedFiles, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during WOPI session cleanup");
                throw;
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WOPI Session Cleanup Service is stopping...");
            
            _timer?.Dispose();
            
            await base.StopAsync(cancellationToken);
        }
    }
}
