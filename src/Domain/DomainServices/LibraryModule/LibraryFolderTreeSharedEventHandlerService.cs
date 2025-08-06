using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryFolderTreeSharedEventHandlerService : ILibraryFolderTreeSharedEventHandlerService
    {
        private readonly ILogger<LibraryFolderTreeSharedEventHandlerService> _logger;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        public LibraryFolderTreeSharedEventHandlerService(
            ILogger<LibraryFolderTreeSharedEventHandlerService> logger,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IObjectArtifactUtilityService objectArtifactUtilityService)
        {
            _logger = logger;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
        }

        public async Task<bool> HandleLibraryFolderTreeSharedEvent(string[] objectArtifactIds)
        {
            //var fileObjectArtifacts = _objectArtifactUtilityService.GetFileObjectArtifacts(objectArtifactIds);
            //if (fileObjectArtifacts.Any())
            //{
            //    await InitiateSharedFormCockpitActivityMetricsGenerationProcess(fileObjectArtifacts);
            //    await InitiateSharedArtifactCockpitActivityMetricsGenerationProcess(fileObjectArtifacts);
            //    await InitiateDraftedDocumentCockpitActivityMetricsGenerationProcess(fileObjectArtifacts);
            //}
            return true;
        }

        private async Task InitiateSharedFormCockpitActivityMetricsGenerationProcess(List<ObjectArtifact> fileObjectArtifacts)
        {
            var formObjectArtifacts = _objectArtifactUtilityService.GetFilteredFormObjectArtifacts(fileObjectArtifacts);
            var formObjectArtifactIds = formObjectArtifacts.Select(o => o.ItemId).ToArray();
            await _cockpitDocumentActivityMetricsGenerationService.OnDocumentShareGenerateActivityMetrics(
                formObjectArtifactIds, $"{CockpitDocumentActivityEnum.PENDING_FORMS_TO_SIGN}");
        }

        private async Task InitiateSharedArtifactCockpitActivityMetricsGenerationProcess(List<ObjectArtifact> fileObjectArtifacts)
        {
            var fileObjectArtifactIds = fileObjectArtifacts.Select(o => o.ItemId).ToArray();
            var formObjectArtifacts = _objectArtifactUtilityService.GetFilteredFormObjectArtifacts(fileObjectArtifacts);
            var formObjectArtifactIds = formObjectArtifacts.Select(o => o.ItemId).ToArray();
            await _cockpitDocumentActivityMetricsGenerationService.OnDocumentShareGenerateActivityMetrics(
                fileObjectArtifactIds.Except(formObjectArtifactIds).ToArray(), 
                $"{CockpitDocumentActivityEnum.DOCUMENTS_ASSIGNED}");
        }

        private async Task InitiateDraftedDocumentCockpitActivityMetricsGenerationProcess(List<ObjectArtifact> fileObjectArtifacts)
        {
            var documentObjectArtifacts = _objectArtifactUtilityService.GetFilteredDocumentObjectArtifacts(fileObjectArtifacts);
            var draftedDocumentObjectArtifacts = _objectArtifactUtilityService.GetFilteredDraftAvailableDocumentObjectArtifacts(documentObjectArtifacts);
            var draftedDocumentObjectArtifactIds = draftedDocumentObjectArtifacts.Select(o => o.ItemId).ToArray();
            await _cockpitDocumentActivityMetricsGenerationService.OnDocFileEditGenerateActivityMetrics(draftedDocumentObjectArtifactIds);
        }
    }
}