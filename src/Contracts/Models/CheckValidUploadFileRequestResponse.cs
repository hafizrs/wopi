using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class CheckValidUploadFileRequestResponse
    {
        public string PraxisClientId { get; set; }
        public bool IsValid { get; set; }
    }
}