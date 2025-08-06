using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class QuickTaskPlanQueryResponse
    {
        public DateTime QuickTaskDate { get; set; }
        public List<RiqsQuickTaskPlanResponse> QuickTaskPlans { get; set; }
    }
} 