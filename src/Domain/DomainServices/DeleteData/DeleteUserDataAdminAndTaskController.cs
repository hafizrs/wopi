using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteUserDataAdminAndTaskController : IDeleteDataByRoleSpecific
    {
        public DeleteUserDataAdminAndTaskController()
        {

        }
        public bool DeleteData(string itemId)
        {
            return true;
        }
    }
}
