using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CreateShiftPlanCommand
    {
        public List<ShiftPlan> ShiftPlans { get; set; }
        public string DepartmentId { get; set; }
    }

    public class ShiftPlan
    {
        public string ShiftId { get; set; }
        public RiqsShift SingleShift { get; set; }
        public DateTime Date { get; set; }
        public List<string> PraxisUserIds { get; set; }
        public bool AssignTask { get; set; }
        public int TimezoneOffsetInMinutes { get; set; }
        public List<DateTime> CloneToDates { get; set; }
        public string Color { get; set; }
        public string Location { get; set; }
        public List<ShiftMaintenanceAttachment> MaintenanceAttachments { get; set; } = new List<ShiftMaintenanceAttachment>();
    }
}
