using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class CreateQuickTaskPlanCommand
    {
        public List<QuickTaskPlan> QuickTaskPlans { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }

    public class QuickTaskPlan
    {
        public string QuickTaskId { get; set; }
        public DateTime QuickTaskDate { get; set; }
        public List<string> AssignedUsers { get; set; }
        public bool AssignTask { get; set; }
        public int TimezoneOffsetInMinutes { get; set; }
        public List<DateTime> CloneToDates { get; set; }
    }
} 