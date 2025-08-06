using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule
{
    public class RiqsAbsencePlan : EntityBase
    {
        public AffectedUserInfo AffectedUserInfo { get; set; }
        public AbsenceTypeInfo AbsenceTypeInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Remarks { get; set; }
        public List<object> Attachments { get; set; }
        public AbsencePlanStatus Status { get; set; }
        public string ReasonToDeny { get; set; }
        public DateTime? StatusUpdatedOn { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }

    public class AbsenceTypeInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
    }

    public class AffectedUserInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
    }

    public enum AbsencePlanStatus
    {
        Pending = 1,
        Approved = 2,
        Denied = 3,
        Cancelled = 4
    }
}