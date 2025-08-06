using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule
{
    public class GetQuickTaskPlanQuery
    {
        public string QuickTaskPlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DepartmentId { get; set; }
    }
} 