namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateLicensingSpecificationCommand
    {
        public string FeatureId { get; set; }
        public string OrganizationId { get; set; }
        public bool IsLicensed { get; set; }
        public bool IsLimitEnable { get; set; }
        public double UsageLimit { get; set; }
        public bool CanOverUse { get; set; }
        public int OverUseLimit { get; set; }
    }
}
