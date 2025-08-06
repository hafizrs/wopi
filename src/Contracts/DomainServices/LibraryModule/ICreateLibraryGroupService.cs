using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ICreateLibraryGroupService
    {
        Task InitiateLibraryGroupCreationAsync(CreateLibraryGroupCommand command);
    }
}
