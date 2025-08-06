using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Bus.Contracts.Event;

namespace Selise.Ecap.ESignature.Service.Events
{
    public class ExternalContractSentAndSignedEvent : BlocksEvent
    {
        public ExternalContractSentAndSignedEvent(
            string documentId,
            string zippedFileId,
            List<FileMap> fileMaps)
        {
            FileMaps = fileMaps;
            DocumentId = documentId;
            ZippedFileId = zippedFileId;
        }

        public string DocumentId { get; private set; }
        public string ZippedFileId { get; private set; }
        public List<FileMap> FileMaps { get; private set; }
    }

    public class FileMap
    {
        public string SourceFileId { get; set; }
        public string SignedFileId { get; set; }
    }
}
