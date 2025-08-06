using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    [BsonIgnoreExtraElements]
    public class RiqsAbsencePlanDetailsResponse
    {
        public RiqsAbsencePlanDetailsResponse() { }
        public RiqsAbsencePlanDetailsResponse(RiqsAbsencePlan absencePlan)
        {
            ItemId = absencePlan.ItemId;
            AffectedUserInfo = absencePlan.AffectedUserInfo;
            CreatedBy = absencePlan.CreatedBy;
            AbsenceTypeInfo = absencePlan.AbsenceTypeInfo;
            StartDate = absencePlan.StartDate;
            EndDate = absencePlan.EndDate;
            Remarks = absencePlan.Remarks;
            Attachments = absencePlan.Attachments;
            Status = absencePlan.Status;
            ReasonToDeny = absencePlan.ReasonToDeny;
            StatusUpdatedOn = absencePlan.StatusUpdatedOn;
            DepartmentId = absencePlan.DepartmentId;
        }

        public string ItemId { get; set; }
        public AffectedUserInfo AffectedUserInfo { get; set; }
        public string CreatedBy { get; set; }
        public AbsenceTypeInfo AbsenceTypeInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Remarks { get; set; }
        public List<object> Attachments { get; set; }
        public AbsencePlanStatus Status { get; set; }
        public string ReasonToDeny { get; set; }
        public DateTime? StatusUpdatedOn { get; set; }
        public string DepartmentId { get; set; }
        public CreatorInfo CreatorInfo { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class RiqsAbsencePlanResponse
    {
        public string ItemId { get; set; }
        public AffectedUserInfo AffectedUserInfo { get; set; }
        public AbsenceTypeInfo AbsenceTypeInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AbsencePlanStatus Status { get; set; }
    }

    public class  CreatorInfo
    {
        public string PraxisUserId { get; set; }
        public string Name { get; set; }
    }
}