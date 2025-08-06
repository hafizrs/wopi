using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactFileShareService : IObjectArtifactFileShareService
    {
        private readonly ILogger<ObjectArtifactFileShareService> _logger;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactShareService _objectArtifactShareService;
        private readonly IObjectArtifactSearchService _objectArtifactSearchService;
        private readonly IServiceClient _serviceClient;

        public ObjectArtifactFileShareService(
            ILogger<ObjectArtifactFileShareService> logger,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactShareService objectArtifactShareService,
            IObjectArtifactSearchService objectArtifactSearchService,
            IServiceClient serviceClient)
        {
            _logger = logger;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactShareService = objectArtifactShareService;
            _objectArtifactSearchService = objectArtifactSearchService;
            _serviceClient = serviceClient;
        }

        public async Task<SearchResult> InitiateSharebjectArtifactFile(ObjectArtifactFileShareCommand command)
        {
            SearchResult response = null;
            var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactSecuredById(command.ObjectArtifactId);
            if (objectArtifact != null)
            {
                var isShared = await _objectArtifactShareService.ShareObjectArtifact(objectArtifact, command);

                if (isShared)
                {
                    PublishLibraryFileSharedEvent(command);
                    response = GetArtifactResponse(command.ObjectArtifactId, command.ViewMode);
                }
            }
            return response;
        }

        private SearchResult GetArtifactResponse(string objectArtifactId, string viewMode)
        {
            var objectArtifactSearchCommand = new ObjectArtifactSearchCommand()
            {
                ObjectArtifactId = objectArtifactId,
                Type = viewMode
            };

            var artifactResponse = _objectArtifactSearchService.InitiateSearchObjectArtifact(objectArtifactSearchCommand);

            return artifactResponse;
        }

        private void PublishLibraryFileSharedEvent(ObjectArtifactFileShareCommand command)
        {
            var fileSharedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFileSharedEvent,
                JsonPayload = JsonConvert.SerializeObject(command)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), fileSharedEvent);

            _logger.LogInformation(
                $"{PraxisEventType.LibraryFileSharedEvent} published  with event:{JsonConvert.SerializeObject(fileSharedEvent)}.");
        }
    }
}