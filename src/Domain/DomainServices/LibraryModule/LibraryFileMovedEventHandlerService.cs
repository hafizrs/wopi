using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspose.Words.Lists;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryFileMovedEventHandlerService : ILibraryFileMovedEventHandlerService
    {
        private readonly ILogger<LibraryFileMovedEventHandlerService> _logger;
        private readonly IObjectArtifactMoveService _objectArtifactMoveService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;

        public LibraryFileMovedEventHandlerService(
            ILogger<LibraryFileMovedEventHandlerService> logger, 
            IObjectArtifactMoveService objectArtifactMoveService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService)
        {
            _logger = logger;
            _objectArtifactMoveService = objectArtifactMoveService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
        }

        public async Task<bool> HandleLibraryFileMovedEvent(string objectArtifactId)
        {
            try
            {
                var artifactIds = new List<string>();
                await _objectArtifactMoveService.MoveChildObjectArtifactsAsync(objectArtifactId, artifactIds);
                await _cockpitDocumentActivityMetricsGenerationService.OnDocumentMoveGenerateObjectArtifactSummary(artifactIds.ToArray());
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error in {HandlerServiceName}. Error Message: {Message}. Error Details: {StackTrace}.", nameof(LibraryFileMovedEventHandlerService), e.Message, e.StackTrace);
                return false;
            }
        }
    }
}