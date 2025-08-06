namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Licensing
{
    public class GetLicensingSpecificationResponse : BaseResponse
    {
        public bool TenantHasLicense { get; set; }
        public bool OrganizationHasLicense { get; set; }
        public bool RoleHasLicense { get; set; }
        public bool UserHasLicense { get; set; }
        public bool CurrentContextHasLicense { get; set; }
        public double AvailableLimit { get; set; }
    }
}
