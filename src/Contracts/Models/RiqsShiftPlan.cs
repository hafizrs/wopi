using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

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
        public string AttachedMaintenanceId { get; set; }
        public string Location { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
}
    