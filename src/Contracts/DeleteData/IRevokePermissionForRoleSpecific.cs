using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface IRevokePermissionForRoleSpecific
    {
        Task RevokePermission(string userId, string personId);
    }
}
