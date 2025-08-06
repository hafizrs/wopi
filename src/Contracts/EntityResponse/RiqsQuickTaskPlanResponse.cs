using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class RiqsQuickTaskPlanResponse
    {
        public RiqsQuickTaskPlanResponse() { }
        public RiqsQuickTaskPlanResponse(RiqsQuickTaskPlan quickTaskPlan)
        {
            ItemId = quickTaskPlan.ItemId;
            QuickTaskDate = quickTaskPlan.QuickTaskDate;
            QuickTaskShift = new RiqsQuickTaskResponse(quickTaskPlan.QuickTaskShift);
            AssignedUsers = quickTaskPlan.AssignedUsers;
            CompletionDate = quickTaskPlan.CompletionDate;
            DepartmentId = quickTaskPlan.DepartmentId;
            OrganizationId = quickTaskPlan.OrganizationId;
        }

        public string ItemId { get; set; }
        public DateTime QuickTaskDate { get; set; }
        public RiqsQuickTaskResponse QuickTaskShift { get; set; }
        public List<string> AssignedUsers { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        public List<PraxisUser> PraxisPersons { get; set; }
        public List<CloneShiftDate> CloneShiftDates { get; set; }
        public List<string> ClonePraxisUserIds { get; set; }
    }
} 