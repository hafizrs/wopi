namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ClientModule
{
    public class UpdateStatusMalfunctionGroupCommand
    {
        public string ItemId { get; set; }
        public bool IsActive { get; set; }
    }
}
