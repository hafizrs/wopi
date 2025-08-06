using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDmsArtifactReapprovalEventHandlerService
    {
        Task InitiateArtifactReapprovalEventHandler();
    }
}
