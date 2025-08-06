namespace Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure
{
    public interface IAppSettings
    {
        string ServiceName { get; set; }
        string BlocksAuditLogQueueName { get; set; }
        string CollaboraBaseUrl { get; set; }
    }
} 