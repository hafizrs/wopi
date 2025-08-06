using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IOrganizationDataProcessService
    {
        Task<(bool isCreated, PraxisOrganization organization)> InitiateNewSubscriptionOrganizationCreateProcess(
            PraxisOrganization organizationData,
            string paymentDetailId);

        Task<bool> ProcessOrganizationStorageSpaceAllocation(PraxisOrganization organizationData);

        Task<bool> InitiateOrganizationCreateUpdateProcess(ProcessOrganizationCreateUpdateCommand command);

        Task<bool> InitiateOrganizationLogoUploadPostProcess(PraxisOrganization organization);
        Task InitiateOrganizationExternalOfficeProcess(ProcessOrganizationExternalOfficeCommand command);
    }
}