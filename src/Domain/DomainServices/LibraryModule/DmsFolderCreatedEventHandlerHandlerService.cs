using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class DmsFolderCreatedEventHandlerHandlerService : IDmsFolderCreatedEventHandlerHandlerService
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactFolderPermissionService _objectArtifactFolderPermissionService;
        private readonly IObjectArtifactShareService _objectArtifactShareService;
        private readonly IObjectArtifactMappingService _objectArtifactMappingService;
        private readonly ILogger<DmsFolderCreatedEventHandlerHandlerService> _logger;

        public DmsFolderCreatedEventHandlerHandlerService(
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFolderPermissionService objectArtifactFolderPermissionService,
            IObjectArtifactShareService objectArtifactShareService,
            IObjectArtifactMappingService objectArtifactMappingService,
            ILogger<DmsFolderCreatedEventHandlerHandlerService> logger
        )
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactFolderPermissionService = objectArtifactFolderPermissionService;
            _objectArtifactShareService = objectArtifactShareService;
            _objectArtifactMappingService = objectArtifactMappingService;
            _logger = logger;
        }

        public async Task<bool> HandleDmsFolderCreatedEvent(ObjectArtifactFolderCreateCommand fileUploadCommand)
        {
            var objectArtifact =
                _objectArtifactUtilityService.GetObjectArtifactById(fileUploadCommand.ObjectArtifactId);

            if (objectArtifact == null) return false;

            //add folder metadata
            var mappingRecord =
                _objectArtifactMappingService.CreateRiqsObjectArtifactMappingPayload(objectArtifact);
            await _objectArtifactMappingService.CreateOrUpdateRiqsObjectArtifactMapping(mappingRecord, false);

            if (objectArtifact.OrganizationId == objectArtifact.ItemId) return true;
            var permissionResponse = await _objectArtifactFolderPermissionService.SetObjectArtifactFolderPermissions(objectArtifact);

            objectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(fileUploadCommand.ObjectArtifactId);
            var response = (_objectArtifactShareService.IsObjectArtifactInASharedDirectory(objectArtifact) && 
                !_objectArtifactUtilityService.IsASecretArtifact(objectArtifact?.MetaData))
                ? await _objectArtifactShareService.InitiateShareWithParentSharedUsers(objectArtifact)
                : permissionResponse;

            await _objectArtifactUtilityService.SetMetaDataProperties(objectArtifact.ItemId);

            return response;
        }

        public async Task<bool> HandleDmsFolderListCreatedEvent(List<ObjectArtifactFolderCreateCommand> folderListCreatedCommand)
        {
            var isAllSuccess = true;
            var taskList = new List<Task<bool>>();

            folderListCreatedCommand ??= new List<ObjectArtifactFolderCreateCommand>();

            folderListCreatedCommand.ForEach(folderCommand =>
            {
                try
                {
                    taskList.Add(HandleDmsFolderCreatedEvent(folderCommand));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception processing ArtifactId: {ArtifactId}", folderCommand.ObjectArtifactId);
                    isAllSuccess = false;
                }
            });

            var totalCount = 0;
            foreach (var batch in taskList.Chunk(100))
            {
                var tasks = await Task.WhenAll(batch);
                totalCount += (tasks?.Where(t => t)?.Count() ?? 0);
            }

            isAllSuccess = totalCount == folderListCreatedCommand?.Count;

            return isAllSuccess;
        }
    }
}
