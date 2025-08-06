using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class LibraryFileEditedByOthersEventHandlerService : ILibraryFileEditedByOthersEventHandlerService
    {
        private readonly ILogger<LibraryFileEditedByOthersEventHandlerService> _logger;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        public LibraryFileEditedByOthersEventHandlerService(
            ILogger<LibraryFileEditedByOthersEventHandlerService> logger,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IObjectArtifactUtilityService objectArtifactUtilityService)
        {
            _logger = logger;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
        }
        public async Task<bool> HandleLibraryFileEditedByOthersEvent(string objectArtifactId)
        {
            try
            {
                var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(objectArtifactId);
                if (objectArtifact?.MetaData == null)
                {
                    _logger.LogInformation(
                        "Exited from : {ServiceName} as object artifact or metadata queried with objectArtifactId: {ItemId} is null",
                        nameof(LibraryFileEditedByOthersEventHandlerService), objectArtifactId);
                    return true;
                }

                if (!objectArtifact.MetaData.TryGetValue("OriginalArtifactId", out var originalArtifactId))
                {
                    _logger.LogInformation(
                        "Exited from : {ServiceName} as OriginalArtifactId is not found in metadata of object artifact with objectArtifactId: {ItemId}",
                        nameof(LibraryFileEditedByOthersEventHandlerService), objectArtifactId);
                    return true;
                }

                var originalArtifact = _objectArtifactUtilityService.GetObjectArtifactById(originalArtifactId.Value);


                await _cockpitDocumentActivityMetricsGenerationService.OnDocFileEditGenerateActivityMetrics(
                    new[] { originalArtifact.ItemId });

                _logger.LogInformation(
                    "Exited from Service: {ServiceName} after handling {HandlerName} with ObjectArtifactId: {ObjectArtifactId}.",
                    nameof(_cockpitDocumentActivityMetricsGenerationService), nameof(LibraryFileEditedByOthersEventHandlerService), objectArtifactId);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in Service: {ServiceName} Error Message: {Message} Error Details: {StackTrace}", nameof(LibraryFileEditedByOthersEventHandlerService), e.Message, e.StackTrace);
            }

            return true;
        }
    }
}