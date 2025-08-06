using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IDocumentUploadAndConversion
    {
        Task<bool> UpdateAndConversion(string fileId, string fileName, byte[] byteArray, string[] tags = null, Dictionary<string, MetaValue> metaData = null, string directoryId = "");
        Task<bool> FileConversion(string fileId, string tagPrefix, string parentEntityId = null, string parentEntityName = null);
    }
}
