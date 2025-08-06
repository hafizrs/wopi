using Selise.Ecap.SC.PraxisMonitor.Contracts.Licensing;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SetLicensingSpecificationCommand = Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.SetLicensingSpecificationCommand;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface ILincensingService
    {
        Task<bool> ProcessStorageLicensing(string organizationId, double totalStorage);
        Task<bool> SetLicensingSpecification(SetLicensingSpecificationCommand command);
        ArcFeatureLicensing GetLicensingSpecification(GetLicensingSpecificationQuery query);
        Task<bool> UpdateLicensingSpecification(UpdateLicensingSpecificationCommand command);
        Task<GetLicensingSpecificationResponse> GetLicensingSpecificationResponse(GetLicensingSpecificationQuery query);
    }
}
