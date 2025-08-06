using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class LibraryRightsAssignCommand
    {
        [Required] public string OrganizationId { get; set; }
        public string DepartmentId { get; set; }
        [Required] public AddRemoveCommand UploadingBody { get; set; }
        [Required] public AddRemoveCommand ApprovingBody { get; set; }
    }
}
