using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule
{
    public class AssignEquipmentAdminsCommand
    {
        [Required]
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        [Required]
        public string CategoryId { get; set; }
        [Required]
        public string SubCategoryId { get; set; }
        [Required]
        public List<string> AddedAdminIds { get; set; }
        [Required]
        public List<string> RemovedAdminIds { get; set; }
        [Required]
        public string EquipmentId { get; set; }
        // [Required] public bool IsDepartmentLevelRight { get; set; } = false;
        [Required] public bool IsOrganizationLevelRight { get; set; } = false;
    }
}
