using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IProcessUserData
    {
        Task<bool> ProcessData(PersonInfo userInformation, PraxisClient praxisClient, string designation);
        Task<bool> ProcessUserCreateUpdateData(PraxisUser praxisUserInformation, FileInformation fileInformation);
    }
}
