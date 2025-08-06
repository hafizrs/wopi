using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class LibraryFileRenamedEventHandlerService : ILibraryFileRenamedEventHandlerService
    {
        private readonly ILogger<LibraryFileRenamedEventHandlerService> _logger;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IRepository _repository;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IObjectArtifactSyncService _objectArtifactSyncService;
        public LibraryFileRenamedEventHandlerService(
            ILogger<LibraryFileRenamedEventHandlerService> logger,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IRepository repository,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IObjectArtifactSyncService artifactSyncService
        )
        {
            _logger = logger;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _repository = repository;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _objectArtifactSyncService = artifactSyncService;
        }

        public async Task<bool> InitiateLibraryFileRenamedAfterEffects(string artifactId)
        {
            var response = false;
            var objectArtifactData = _objectArtifactUtilityService.GetObjectArtifactById(artifactId);

            if (objectArtifactData != null)
            {
                await _objectArtifactSyncService.UpdateEntityDependencyAsync(new List<string> { artifactId }, objectArtifactData);
                await _cockpitDocumentActivityMetricsGenerationService.OnDocumentRenameGenerateObjectArtifactSummary(artifactId);
                return true;
            }
            return response;
        }

    }

    
}