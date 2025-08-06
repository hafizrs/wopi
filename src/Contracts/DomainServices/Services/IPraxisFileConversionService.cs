namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisFileConversionService
    {
        void AddConvertedFileMaps(string sourceFileId);

        void MarkToDeleteConvertedFileMaps(string orgFileId);
    }
}
