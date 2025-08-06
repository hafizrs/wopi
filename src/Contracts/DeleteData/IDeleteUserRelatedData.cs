using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface IDeleteUserRelatedData
    {
        Task<(bool, string, string)> DeleteData(string userId);
    }
}
