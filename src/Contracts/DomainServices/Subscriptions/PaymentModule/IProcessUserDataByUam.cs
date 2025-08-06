using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IProcessUserDataByUam
    {
        Task<(bool, string userId)> SaveData(PersonInformation userInformation);
        Task<bool> UpdateData(PersonInformation userInformation);
    }
}
