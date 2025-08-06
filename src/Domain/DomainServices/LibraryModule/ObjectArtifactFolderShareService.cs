using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactFolderShareService : IObjectArtifactFolderShareService
    {
        private readonly ILogger<ObjectArtifactFolderShareService> _logger;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactShareService _objectArtifactShareService;
        private readonly IObjectArtifactSearchService _objectArtifactSearchService;
        private readonly IServiceClient _serviceClient;

        public ObjectArtifactFolderShareService(
            ILogger<ObjectArtifactFolderShareService> logger,
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

        public async Task<SearchResult> InitiateSharebjectArtifactFolder(ObjectArtifactFolderShareCommand command)
        {
            SearchResult response = null;
            var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactSecuredById(command.ObjectArtifactId);

            if (objectArtifact != null)
            {
                var isShared = await _objectArtifactShareService.ShareObjectArtifact(objectArtifact, command);
                if (isShared)
                {
                    response = GetArtifactResponse(command.ObjectArtifactId, command.ViewMode);
                    PublishLibraryFolderSharedEvent(command);
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

        private void PublishLibraryFolderSharedEvent(ObjectArtifactFileShareCommand command)
        {
            var folderSharedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFolderSharedEvent,
                JsonPayload = JsonConvert.SerializeObject(command)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), folderSharedEvent);

            _logger.LogInformation(
                $"{PraxisEventType.LibraryFolderSharedEvent} publiushed  with event:{JsonConvert.SerializeObject(folderSharedEvent)}.");
        }
    }
}