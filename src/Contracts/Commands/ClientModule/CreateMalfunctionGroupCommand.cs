using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Selise.Ecap.SC.PraxisMonitor.Commands.ClientModule
{
    public class CreateMalfunctionGroupCommand
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        [Required]
        public string OrganizationId { get; set; }
        [Required]
        public string ClientId { get; set; }
        public List<string> ControllingGroup { get; set; } = new();
        public List<string> ControlledGroup { get; set; } = new();
        public List<string> MalfunctionTypes { get; set; } = new();
        public List<string> MalfunctionSubTypes { get; set; } = new();
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }
}
