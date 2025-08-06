using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule
{
    public class RiqsAbsenceType
    {
        [BsonId]
        public string ItemId { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public string DepartmentId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }

    }
}