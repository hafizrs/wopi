using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule
{
    [BsonIgnoreExtraElements]
    public class PraxisEquipmentRight : EntityBase
    {
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        public List<UserPraxisUserIdPair> AssignedAdmins { get; set; }
        public string EquipmentId { get; set; }
        //public bool IsDepartmentLevelRight { get; set; }
        public bool IsOrganizationLevelRight { get; set; }
    }
}
