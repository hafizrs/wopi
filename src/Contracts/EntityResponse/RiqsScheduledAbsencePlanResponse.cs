using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    [BsonIgnoreExtraElements]
    public class RiqsScheduledAbsencePlanResponse
    {
        public RiqsScheduledAbsencePlanResponse() { }
        public RiqsScheduledAbsencePlanResponse(RiqsAbsencePlan absencePlan)
        {
            ItemId = absencePlan.ItemId;
            AffectedUserInfo = absencePlan.AffectedUserInfo;
            AbsenceTypeInfo = absencePlan.AbsenceTypeInfo;
            StartDate = absencePlan.StartDate;
            EndDate = absencePlan.EndDate;
            Status = absencePlan.Status;
        }

        public string ItemId { get; set; }
        public AffectedUserInfo AffectedUserInfo { get; set; }
        public AbsenceTypeInfo AbsenceTypeInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AbsencePlanStatus Status { get; set; }
    }
}