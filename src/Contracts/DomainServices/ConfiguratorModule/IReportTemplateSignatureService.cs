using Selise.Ecap.ESignature.Service.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule
{
    public interface IReportTemplateSignatureService
    {
        Task GenerateSignatureUrlAsync(string relatedEntityId, string externalUserId = null);
        Task<bool> CreateSignatureRequestAsync(PraxisGeneratedReportTemplateConfig reportConfig, string reportId = null, string externalUserId = null);
        Task<bool> UpdateSignatureUrlAsync(ProcessExternalSignResponse response);
        Task<bool> CompleteSignatureProcessAsync(ExternalContractSentAndSignedEvent response);


        Task<bool> IsReportExistsAsync(string reportTemplateId);
        Task<EntitySignatureMappingResponse> GetSignatureUrlAsync(string relatedEntityId);
        Task<bool> IsSignatureMappingExistsAsync(string documentId);

        Task<string> GetRelatedEntityIdFromSignatureMappingByDocumentId(string documentId);
    }
}
