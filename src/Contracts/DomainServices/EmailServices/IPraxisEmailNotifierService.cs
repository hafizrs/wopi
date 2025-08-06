using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EmailServices
{
    public interface IPraxisEmailNotifierService
    {
        Task<bool> SendCreateOrganizationEmail(List<string> emailList, Dictionary<string, string> dataContext, string emailTemplate = null);
    }
}
