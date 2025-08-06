using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IProcessUserInformation
    {
        Task<bool> ProcessData(PersonInfo userInformation, PraxisClient primaryDepartment, string designation);
        Task<bool> ProcessData(PraxisUser praxisUserInformation, FileInformation fileInformation);
    }
}
