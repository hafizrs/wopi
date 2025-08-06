using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class CloneQuickTaskPlansCommand
    {
        [Required] public List<DateTime> CloneToDates { get; set; }
        [Required] public List<string> QuickTaskPlanIds { get; set; }
        [Required] public string DepartmentId { get; set; }
    }
} 