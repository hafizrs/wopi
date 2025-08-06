namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetOwnClientListQuery
    {
        public string LoggedInPraxisUserId { get; set; }
        public bool? ForProcessGuideForm { get; set; }
    }
}