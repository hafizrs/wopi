using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule
{
    [BsonIgnoreExtraElements]
    public class RiqsLibraryControlMechanism : EntityBase
    {
        public string OrganizationId { get; set; }
        public string DepartmentId { get; set; }
        public string ControlMechanismName { get; set; }
        public List<UserPraxisUserIdPair> ApprovalAdmins { get; set; }
        public List<UserPraxisUserIdPair> UploadAdmins { get; set; }
    }

    public class UserPraxisUserIdPair
    {
        public string UserId { get; set; }
        public string PraxisUserId { get; set; }
    }
}
