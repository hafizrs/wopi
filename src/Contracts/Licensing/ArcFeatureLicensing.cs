using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Licensing
{
    public class ArcFeatureLicensing
    {
        [BsonId]
        public string ItemId { get; set; }
        public string FeatureId { get; set; }
        public string OrganizationId { get; set; }
        public bool IsLicensed { get; set; }
        public bool IsLimitEnable { get; set; }
        public double UsageLimit { get; set; }
        public double Usage { get; set; }
        public bool CanOverUse { get; set; }
        public double OverUseLimit { get; set; }
        public bool RolePermissionRequired { get; set; }
        public bool UserPermissionRequired { get; set; }
        public bool HasExpiryDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string[] AllowedRoleIds { get; set; }
        public string[] AllowedUserIds { get; set; }
        public bool IsLicensingEnable { get; set; }
        public string Type { get; set; }
        public string AppName { get; set; }
    }
}
