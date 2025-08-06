using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class DmsFileUploadResponse
    {
        public string UploadStorageUri { get; set; }
        public int StatusCode { get; set; }
    }
}
