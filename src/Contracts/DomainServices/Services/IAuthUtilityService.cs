using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.Contracts.DomainServices
{
    public interface IAuthUtilityService
    {
        Task<string> GetAdminToken();
    }
}