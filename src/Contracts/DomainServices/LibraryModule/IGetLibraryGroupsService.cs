using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IGetLibraryGroupsService
    {
        Task<GetLibraryGroupsResponse> GetLibraryGroupsAsync(GetLibraryGroupsQuery query);
    }
}
