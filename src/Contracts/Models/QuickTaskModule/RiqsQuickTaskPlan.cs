using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule
{
    public class RiqsQuickTaskPlan : EntityBase
    {
        public DateTime QuickTaskDate { get; set; }
        public int TimezoneOffsetInMinutes { get; set; }
        public RiqsQuickTask QuickTaskShift { get; set; }
        public List<string> AssignedUsers { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
} 