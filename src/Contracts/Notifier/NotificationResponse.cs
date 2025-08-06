namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier
{
    public class NotificationResponse
    {
        public bool Success { get; set; }
        public string Result { get; set; }
        public string ValidationResult { get; set; }
        public string ErrorMessage { get; set; }
    }
}