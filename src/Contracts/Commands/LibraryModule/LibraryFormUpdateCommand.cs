using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class LibraryFormUpdateCommand
    {
        [Required] public string ObjectArtifactId { get; set; }
        [Required] public bool IsDraft { get; set; }
    }
}
