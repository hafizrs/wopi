namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ListQueryModel
    {
        public string Filter { get; set; }
        public string Sort { get; set; }
        public int PageNumber { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }
}
