using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class ShiftPlanQueryResponse
    {
        public DateTime ShiftDate { get; set; }
        public List<RiqsShiftPlanResponse> ShiftPlans { get; set; }
    }
}
