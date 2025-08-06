using System.IO;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IDocGenerationService
    {
        byte[] PrepareDocumentFromHtmlStream(Stream contentStream);
        byte[] PrepareHtmlFromObjectArtifactDocumentStream(Stream contentStream);
        byte[] PrepareObjectArtifactDocumentFromHtmlStream(Stream contentStream);
    }
}
