using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryFileDeletedEventHandlerService : ILibraryFileDeletedEventHandlerService
    {
        private readonly ILogger<LibraryFileDeletedEventHandlerService> _logger;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IDependencyManagementService _dependencyManagementService;


        public LibraryFileDeletedEventHandlerService(
            ILogger<LibraryFileDeletedEventHandlerService> logger,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IDependencyManagementService dependencyManagementService)
        {
            _logger = logger;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _dependencyManagementService = dependencyManagementService;
        }

        public async Task<bool> HandleLibraryFileDeletedEvent(List<string> artifactIds)
        {
            await _cockpitDocumentActivityMetricsGenerationService.OnDocumentDeleteGenerateActivityMetrics(artifactIds?.ToArray() ?? new string[] {});
            await _dependencyManagementService.HandleFileDeletionAsync(artifactIds ?? new List<string>());
            return true;
        }
    }
}