using System;
using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    [BsonIgnoreExtraElements]
    public class EmailTemplate : EntityBase
    {
        public override DateTime CreateDate { get; set; }
        public override string CreatedBy { get; set; }
        public override string Language { get; set; }
        public override DateTime LastUpdateDate { get; set; }
        public override string LastUpdatedBy { get; set; }
        public string MailConfigurationId { get; set; }
        public string Name { get; set; }
        public string TemplateBody { get; set; }
        public string TemplateSubject { get; set; }
        public override string TenantId { get; set; }
    }
}