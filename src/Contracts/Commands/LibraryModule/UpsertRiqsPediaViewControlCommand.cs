namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpsertRiqsPediaViewControlCommand
    {
        public bool ViewState { get; set; }
        public string NotificationSubscriptionId { get; set; }
        public string ActionName { get; set; }
        public string Context { get; set; }
    }
}
