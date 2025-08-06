using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CloneShiftPlansCommand
    {
        [Required] public List<DateTime> CloneToDates { get; set; }
        [Required] public List<string> ShiftPlanIds { get; set; }
        [Required] public string DepartmentId { get; set; }
    }
}
