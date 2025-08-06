using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IUserActivityService
    {
        Task SaveUserActivity(string userId);
        Task<object> GetUserActivity(string userId, string action);
    }
}
