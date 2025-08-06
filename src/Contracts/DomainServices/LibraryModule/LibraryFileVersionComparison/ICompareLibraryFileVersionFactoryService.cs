using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison
{
    public interface ICompareLibraryFileVersionFactoryService
    {
        ICompareFileVersionFromStreamService GetFileCompareService(LibraryFileTypeEnum fileType);
    }

}
