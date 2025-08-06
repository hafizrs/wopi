namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona
{
    public interface ISaveDataToPlatformDictionary
    {
        bool SaveOrganizationInfoWithPersonaRole(string roleName, string organizationId);
    }
}
