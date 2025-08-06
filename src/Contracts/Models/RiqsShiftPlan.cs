using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsShiftPlan : EntityBase
    {
        public DateTime ShiftDate { get; set; }
        public RiqsShift Shift { get; set; }
        public bool IsProcessGuidCreated { get; set; }
        public string ProcessGuideId { get; set; }
        public List<string> PraxisUserIds { get; set; }
        public int TimezoneOffsetInMinutes { get; set; }
        public string Color { get; set; }
        public List<ShiftMaintenanceAttachment> AttachedMaintenances { get; set; }
        public string Location { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }

    public class ShiftMaintenanceAttachment
    {
        public string MaintenanceId { get; set; }
        public string Location { get; set; }
        public string ExactLocation { get; set; }
        public DateTime ExecutionDate { get; set; }
        public List<string> ExecutivePersonIds { get; set; } = new List<string>();
    }
}
