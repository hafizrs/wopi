using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CreateStandardLibraryFormCommand : LibraryFormCloneCommand
    {
        [Required] public string FileName { get; set; }
        
    }
}
