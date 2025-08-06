using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison
{
    public interface ICompareFileVersionFromStreamService
    {
        Task<byte[]> CompareFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream);
        Task<byte[]> CompareDeleteFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream);
        Task<byte[]> CompareUpdateFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream);
    }
}
