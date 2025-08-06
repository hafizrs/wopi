using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class RiqsShiftPlanResponse
    {
        public RiqsShiftPlanResponse() { }

        public RiqsShiftPlanResponse(RiqsShiftPlan shiftPlan)
        {
            ItemId = shiftPlan.ItemId;
            ShiftDate = shiftPlan.ShiftDate;
            Shift = new RiqsShiftResponse(shiftPlan.Shift);
            IsProcessGuidCreated = shiftPlan.IsProcessGuidCreated;
            PraxisUserIds = shiftPlan.PraxisUserIds;
        }

        public string ItemId { get; set; }
        public string ProcessGuideId { get; set; }
        public DateTime ShiftDate { get; set; }
        public RiqsShiftResponse Shift { get; set; }
        public bool IsProcessGuidCreated { get; set; }
        public List<string> PraxisUserIds { get; set; }
        public virtual List<PraxisUser> PraxisPersons { get; set; }
        public List<CloneShiftDate> CloneShiftDates { get; set; }
        public List<string> ClonePraxisUserIds { get; set; }
        public string Color { get; set; }
        public string DepartmentId { get; set; }
    }

    public class CloneShiftDate
    {
        public string ItemId { get; set; }
        public DateTime ShiftDate { get; set; }
    }
}
