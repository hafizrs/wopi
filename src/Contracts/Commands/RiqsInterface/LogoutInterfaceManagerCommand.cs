using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class LogoutInterfaceManagerCommand
    {
        [Required]
        public string Provider { get; set; }
    }
}
