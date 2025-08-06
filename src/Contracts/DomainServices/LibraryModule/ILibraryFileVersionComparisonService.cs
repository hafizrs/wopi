using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFileVersionComparisonService
    {
        Task<bool> HandleLibraryFileVersionComparison(string objectArtifactId);
        Task<bool> HandleLibraryFileVersionComparisonByFileStorageId(string fileStorageId);
    }
}
