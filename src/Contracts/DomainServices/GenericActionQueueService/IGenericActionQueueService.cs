using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.GenericActionQueueService
{
    public interface IGenericActionQueueService
    {

        Task CreateHtmlFileIdFromArtifact(string objectArtifactId);
    }
}
