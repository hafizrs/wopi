using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IUpdateOrgTypeChangePermissionService
    {
        Task<bool> UpdateOrgTypeChangePermission(string clientId, string paymentDetailId);
    }
}
