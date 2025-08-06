namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class SetLicensingSpecificationCommand
    {
        public string FeatureId { get; set; }
        public string OrganizationId { get; set; }
        public bool IsLicensed { get; set; }
        public bool IsLimitEnable { get; set; }
        public double UsageLimit { get; set; }
        public int Usage { get; set; }
        public bool CanOverUse { get; set; }
        public int OverUseLimit { get; set; }
        public bool HasExpiryDate { get; set; }
        public bool RolePermissionRequired { get; set; }
        public bool UserPermissionRequired { get; set; }
    }
}