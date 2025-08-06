using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Linq;
using System.Linq.Expressions;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.Entities;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryRightsUpdatedEventHandlerService : ILibraryRightsUpdatedEventHandlerService
    {
        private readonly ILogger<LibraryRightsUpdatedEventHandlerService> _logger;
        private readonly IRepository _repository;
        private readonly IChangeLogService _changeLogService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IObjectArtifactPermissionHelperService _objectArtifactPermissionHelperService;
        private readonly IObjectArtifactShareService _objectArtifactShareService;
        private readonly IDmsArtifactReapprovalEventHandlerService _dmsArtifactReapprovalEventHandlerService;

        public LibraryRightsUpdatedEventHandlerService(
            ILogger<LibraryRightsUpdatedEventHandlerService> logger,
            IRepository repository,
            IChangeLogService changeLogService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IObjectArtifactPermissionHelperService objectArtifactPermissionHelperService,
            IObjectArtifactShareService objectArtifactShareService,
            IDmsArtifactReapprovalEventHandlerService dmsArtifactReapprovalEventHandlerService)
        {
            _logger = logger;
            _repository = repository;
            _changeLogService = changeLogService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _objectArtifactPermissionHelperService = objectArtifactPermissionHelperService;
            _objectArtifactShareService = objectArtifactShareService;
            _dmsArtifactReapprovalEventHandlerService = dmsArtifactReapprovalEventHandlerService;
        }

        public async Task<bool> HandleLibraryRightsUpdatedEvent(RiqsLibraryControlMechanism control)
        {
            var orgId = control?.OrganizationId;
            if (!string.IsNullOrEmpty(control?.DepartmentId))
            {
                var dept = _repository.GetItem<PraxisClient>(c => c.ItemId == control.DepartmentId);
                if (!string.IsNullOrEmpty(dept?.ParentOrganizationId)) orgId = dept?.ParentOrganizationId;
            }
            var response = await InitiateObjectArtifactUpdate(control, orgId);
            if (!string.IsNullOrEmpty(orgId))
            {
                await _cockpitDocumentActivityMetricsGenerationService.OnOrganizationLibraryRightsUpdateGenerateActivityMetrics(orgId);
                await _dmsArtifactReapprovalEventHandlerService.InitiateArtifactReapprovalEventHandler();
            }
            return response;
        }

        public async Task<bool> InitiateObjectArtifactUpdate(RiqsLibraryControlMechanism control, string organizationId)
        {
            List<Task<bool>> listOfTasks = new List<Task<bool>>();

            int pageNumber = 0, pageSize = LibraryModuleConstants.LibraryPageLimit, artifactCount = 0;
            var formFillStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS.ToString()];
            var fileTypeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"];
            Expression<Func<ObjectArtifact, bool>> filter =
                o => o.OrganizationId == organizationId && !o.IsMarkedToDelete &&
                    !(o.MetaData != null && o.MetaData[fileTypeKey] != null && o.MetaData[fileTypeKey].Value == ((int)LibraryFileTypeEnum.FORM).ToString() &&
                    o.MetaData[formFillStatusKey] != null && o.MetaData[formFillStatusKey].Value != ((int)FormFillStatus.COMPLETE).ToString());

            var libraryControlMechanisms = new List<RiqsLibraryControlMechanism> { control };

            if (!string.IsNullOrEmpty(control.DepartmentId))
            {
                var isOrgLevelKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_ORG_LEVEL.ToString()];

                filter = o => o.OrganizationId == organizationId && !o.IsMarkedToDelete && o.MetaData != null &&
                    !(o.MetaData != null && o.MetaData[fileTypeKey] != null && o.MetaData[fileTypeKey].Value == ((int)LibraryFileTypeEnum.FORM).ToString() &&
                    o.MetaData[formFillStatusKey] != null && o.MetaData[formFillStatusKey].Value != ((int)FormFillStatus.COMPLETE).ToString()) &&
                    o.MetaData["DepartmentId"] != null && o.MetaData["DepartmentId"].Value == control.DepartmentId &&
                    !(o.MetaData[isOrgLevelKey] != null && o.MetaData[isOrgLevelKey].Value == ((int)LibraryBooleanEnum.TRUE).ToString());
            }

            while (true)
            {
                var objectArtifacts = GetPaginatedObjectArtifacts(filter, pageNumber, pageSize);
                listOfTasks.Add(UpdateBulkObjectArtifact(objectArtifacts));
                artifactCount = objectArtifacts.Count();
                if (artifactCount < pageSize)
                {
                    break;
                }
                pageNumber++;
            }

            var response = await Task.WhenAll<bool>(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        private async Task<bool> UpdateBulkObjectArtifact(List<ObjectArtifact> objectArtifacts)
        {
            List<Task<bool>> listOfTasks = new List<Task<bool>>();

            objectArtifacts.ForEach(artifact =>
            {
                listOfTasks.Add(UpdateSingleObjectArtifact(artifact));
            });

            var response = await Task.WhenAll<bool>(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        public List<ObjectArtifact> GetPaginatedObjectArtifacts(
            Expression<Func<ObjectArtifact, bool>> filter,
            int pageNumber, int pageSize)
        {
            var artifacts =
            _repository.GetItems(filter)?
                .Skip(pageNumber * pageSize)
                .Take(pageSize)?
                .ToList();

            return artifacts;
        }

        private async Task<bool> UpdateSingleObjectArtifact(ObjectArtifact objectArtifact)
        {
            var updates = PrepareObjectArtifactUpdates(objectArtifact);
            var builder = Builders<BsonDocument>.Filter;
            var updateFilters = builder.Eq("_id", objectArtifact.ItemId);

            _logger.LogInformation(
                $"------------------------------------------------------------------  " +
                $"Going to update ObjectArtifact with id -> {objectArtifact.ItemId},  " +
                $"Updates: {JsonConvert.SerializeObject(updates, Formatting.Indented)}  ");

            var response = await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, updates);
            if (!response)
            {
                _logger.LogInformation(
                $"------------------------------------------------------------------  " +
                $"ObjectArtifact update was't successful with id -> -> {objectArtifact.ItemId},  ");
            }
            return response;
        }

        private Dictionary<string, object> PrepareObjectArtifactUpdates(
            ObjectArtifact objectArtifact)
        {
            var authorizedUserIds = _objectArtifactPermissionHelperService
                        .GetObjectArtifactAuthorizedIds(objectArtifact, onlyDeptLevel: _objectArtifactUtilityService.IsASecretArtifact(objectArtifact.MetaData));

            if (!string.IsNullOrEmpty(objectArtifact.OwnerId)) authorizedUserIds = authorizedUserIds.Union(new string[] { objectArtifact.OwnerId }).Distinct().ToArray();

            var writeIds = authorizedUserIds;
            var sharedReadIds = _objectArtifactShareService.GetSharedIdsAllowedToRead(objectArtifact.SharedOrganizationList);
            var sharedUpdateIds = _objectArtifactShareService.GetSharedIdsAllowedToUpdate(objectArtifact.SharedOrganizationList);

            var idsAllowedToRead = (sharedReadIds).Union(authorizedUserIds).Distinct().ToArray();
            var idsAllowedToUpdate = (sharedUpdateIds).Union(authorizedUserIds).Distinct().ToArray();
            var idsAllowedToWrite = writeIds;
            var idsAllowedToDelete = authorizedUserIds;

            var updates = new Dictionary<string, object>
            {
                { nameof(objectArtifact.IdsAllowedToRead) , idsAllowedToRead },
                { nameof(objectArtifact.IdsAllowedToUpdate), idsAllowedToUpdate },
                { nameof(objectArtifact.IdsAllowedToWrite), idsAllowedToWrite },
                { nameof(objectArtifact.IdsAllowedToDelete), idsAllowedToDelete }
            };

            return updates;
        }
        private List<PraxisUser> GetPraxisUsersByUserIds(List<string> userIds, string deptId)
        {
            if (string.IsNullOrWhiteSpace(deptId)) return new List<PraxisUser>();
            return _repository.GetItems<PraxisUser>(pu => (userIds.Contains(pu.UserId) && pu.ClientList.Any(c => c.ClientId == deptId))
            )?.Select(pu => new PraxisUser()
            {
                ItemId = pu.ItemId,
                UserId = pu.UserId,
                DisplayName = pu.DisplayName,
                Roles = pu.Roles,
                IsMarkedToDelete = pu.IsMarkedToDelete
            })?.ToList() ?? new List<PraxisUser>();
        }

    }
}