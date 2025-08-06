using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

public interface ILibraryStandardDocumentService
{
    Task UpdateDocumentEditRecordForChildStandardFile();
    Task<bool> UpdateDocumentEditRecordHistory(DocumentEditMappingRecord documentEditMappingRecord);
}