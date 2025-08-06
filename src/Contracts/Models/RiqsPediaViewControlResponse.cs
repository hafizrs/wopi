namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsPediaViewControlResponse
    {
        public string PraxisUserId { get; set; }
        public string UserId { get; set; }
        public bool ViewState { get; set; }
        public bool IsShowViewState { get; set; }
        public bool IsAdminViewEnabled { get; set; }
    }
}
