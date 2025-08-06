namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class CirsAdminsResponse
    {
        public string PraxisUserId { get; set; }
        public string DisplayName { get; set; }
        public bool IsAAdmin { get; set; }
        public bool IsChangeable { get; set; }
    }
}