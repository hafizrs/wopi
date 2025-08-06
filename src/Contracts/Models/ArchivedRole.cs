using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ArchivedRole
    {
        [BsonId]
        public string ItemId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string EntityName { get; set; }
        public string EntityItemId { get; set; }
        public string[] ArchivedRolesAllowedToRead { get; set; }
        public string[] ArchivedIdsAllowedToRead { get; set; }
        public string[] ArchivedRolesAllowedToWrite { get; set; }
        public string[] ArchivedIdsAllowedToWrite { get; set; }
        public string[] ArchivedRolesAllowedToUpdate { get; set; }
        public string[] ArchivedIdsAllowedToUpdate { get; set; }
        public string[] ArchivedRolesAllowedToDelete { get; set; }
        public string[] ArchivedIdsAllowedToDelete { get; set; }
        public string ActionType { get; set; }
        public List<RelatedEntity> RelatedEntityList { get; set; }
    }

    public class RelatedEntity
    {
        public string EntityName { get; set; }
        public string EntityItemId { get; set; }
        public List<string> RelatedProperties { get; set; }
    }
}