using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Globalization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactApprovalService : IObjectArtifactApprovalService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly IChangeLogService _changeLogService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactFilePermissionService _objectArtifactFilePermissionService;
        private readonly IObjectArtifactPermissionHelperService _objectArtifactPermissionHelperService;
        private readonly IObjectArtifactShareService _objectArtifactShareService;
        private readonly IObjectArtifactSearchService _objectArtifactSearchService;
        private readonly ILogger<ObjectArtifactApprovalService> _logger;
        private readonly IObjectArtifactMappingService _objectArtifactMappingService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly IServiceClient _serviceClient;

        public ObjectArtifactApprovalService(
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            IChangeLogService changeLogService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFilePermissionService objectArtifactFilePermissionService,
            IObjectArtifactPermissionHelperService objectArtifactPermissionHelperService,
            IObjectArtifactShareService objectArtifactShareService,
            IObjectArtifactSearchService objectArtifactSearchService,
            IObjectArtifactMappingService objectArtifactMappingService,
            ILogger<ObjectArtifactApprovalService> logger,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            IServiceClient serviceClient
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _changeLogService = changeLogService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactFilePermissionService = objectArtifactFilePermissionService;
            _objectArtifactPermissionHelperService = objectArtifactPermissionHelperService;
            _objectArtifactShareService = objectArtifactShareService;
            _objectArtifactSearchService = objectArtifactSearchService;
            _objectArtifactMappingService = objectArtifactMappingService;
            _logger = logger;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _serviceClient = serviceClient;
        }

        public async Task<SearchResult> InitiateObjectArtifactApprovalProcess(ObjectArtifactApprovalCommand command)
        {
            var response = new SearchResult(null, null);
            var objectArtifact = GetObjectArtifact(command.ObjectArtifactId);
            if (objectArtifact != null)
            {
                var haveNextReapproveDateKey = _objectArtifactAuthorizationCheckerService.HaveNextReapproveDateKey(objectArtifact?.MetaData);
                var isUpdated = await UpdateObjectArtifact(objectArtifact);
                if (isUpdated)
                {
                    if (!haveNextReapproveDateKey)
                    {
                        if (objectArtifact.MetaData != null && _objectArtifactUtilityService.IsADocument(objectArtifact?.MetaData, true))
                        {
                            await _objectArtifactFilePermissionService.SetObjectArtifactFilePermissions(objectArtifact, ObjectArtifactEvent.FILE_APPROVED);
                            PublishLibraryFileApprovedEvent(objectArtifact.ItemId);
                            return GetObjectArtifactById(command.ObjectArtifactId);
                        }

                        if (objectArtifact.MetaData != null &&
                            _objectArtifactUtilityService.IsASavedDraftedChildDocument(objectArtifact.MetaData))
                        {
                            await _objectArtifactFilePermissionService.SetObjectArtifactFilePermissions(objectArtifact, ObjectArtifactEvent.FILE_APPROVED);
                            PublishLibraryFileApprovedEvent(objectArtifact.ItemId);
                            return GetObjectArtifactById(command.ObjectArtifactId);
                        }

                        if (_objectArtifactUtilityService.IsAApprovedObjectArtifact(objectArtifact.MetaData))
                        {
                            await _objectArtifactFilePermissionService.SetObjectArtifactFilePermissions(objectArtifact, ObjectArtifactEvent.FILE_APPROVED);
                            if (_objectArtifactShareService.IsObjectArtifactInASharedDirectory(objectArtifact))
                            {
                                objectArtifact = GetObjectArtifact(command.ObjectArtifactId);
                                await _objectArtifactShareService.InitiateShareWithParentSharedUsers(objectArtifact);
                                PublishLibraryFileSharedEvent(objectArtifact.ItemId);
                            }
                        }

                        PublishLibraryFileApprovedEvent(objectArtifact.ItemId);
                        return GetObjectArtifactById(command.ObjectArtifactId);
                    }
                    else
                    {
                        return GetObjectArtifactById(command.ObjectArtifactId);
                    }
                }
            }

            return response;
        }

        private void PublishLibraryFileApprovedEvent(string objectArtifactId)
        {
            var fileApprovedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFileApprovedEvent,
                JsonPayload = JsonConvert.SerializeObject(objectArtifactId)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), fileApprovedEvent);
        }

        private void PublishLibraryFileSharedEvent(string objectArtifactId)
        {
            var fileSharedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFileSharedEvent,
                JsonPayload = JsonConvert.SerializeObject(objectArtifactId)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), fileSharedEvent);

            _logger.LogInformation(
                $"{PraxisEventType.LibraryFileSharedEvent} publiushed  with event:{JsonConvert.SerializeObject(fileSharedEvent)}.");
        }

        private ObjectArtifact GetObjectArtifact(string id)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return _repository.GetItem<ObjectArtifact>(o =>
                o.ItemId == id &&
                !o.IsMarkedToDelete &&
                (o.RolesAllowedToRead.Any(r => securityContext.Roles.Contains(r)) || o.IdsAllowedToRead.Contains(securityContext.UserId)) &&
                (
                    o.RolesAllowedToUpdate.Any(r => securityContext.Roles.Contains(r)) || o.IdsAllowedToUpdate.Contains(securityContext.UserId)
                    || (o.RolesAllowedToWrite != null && o.RolesAllowedToWrite.Any(r => securityContext.Roles.Contains(r)))
                    || (o.IdsAllowedToWrite != null && o.IdsAllowedToWrite.Contains(securityContext.UserId))
                ));
        }

        private async Task<bool> UpdateObjectArtifact(ObjectArtifact objectArtifact)
        {
            var update = PrepareObjectArtifactUpdate(objectArtifact);
            if (update == null) return false;

            var builder = Builders<BsonDocument>.Filter;
            var updateFilters = builder.Eq("_id", objectArtifact.ItemId);
            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, update);
        }

        private Dictionary<string, object> PrepareObjectArtifactUpdate(ObjectArtifact objectArtifact)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var currentTime = DateTime.UtcNow;

            var metaDataUpdate = PrepareObjectArtifactMetaDataUpdate(objectArtifact, currentTime);
            PrepareObjectArtifactActivitySummaryUpdate(objectArtifact, currentTime);
            if (metaDataUpdate == null) return null;

            var updates = new Dictionary<string, object>
            {
                { "LastUpdateDate",  currentTime },
                { "LastUpdatedBy", securityContext.UserId },
                { "MetaData", metaDataUpdate },
                { "ActivitySummary", objectArtifact.ActivitySummary }
            };

            return updates;
        }

        private IDictionary<string, MetaValuePair> PrepareObjectArtifactMetaDataUpdate(ObjectArtifact artifact, DateTime currentTime)
        {
            var metaData = artifact.MetaData;

            var stringDataType = LibraryModuleConstants.ObjectArtifactMetaDataKeyTypes[$"{ObjectArtifactMetaDataKeyTypeEnum.STRING}"];

            var controlMechanismData = LibraryControlMechanismConstant.GetLibraryControlMechanismDataByOrgId(artifact.OrganizationId);

            if (string.IsNullOrEmpty(controlMechanismData?.ControlMechanismName))
            {
                _logger.LogWarning("No controlMechanism applied");
                return metaData;
            }

            var approvalStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS}"];
            var approvedStatusValue = ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString();
            var approvalStatusValue = _objectArtifactMappingService
                .UpdateAndGetApprovalStatusFromMapping(artifact, controlMechanismData?.ControlMechanismName, currentTime).Result;

            var metaDataApprovalStatusValue = new MetaValuePair()
            {
                Type = stringDataType,
                Value = approvalStatusValue
            };

            metaData ??= new Dictionary<string, MetaValuePair>() { };
            if (metaData.TryGetValue(approvalStatusKey, out MetaValuePair _))
            {
                metaData[approvalStatusKey] = metaDataApprovalStatusValue;
            }
            else
            {
                metaData.Add(approvalStatusKey, metaDataApprovalStatusValue);
            }

            if (approvalStatusValue == approvedStatusValue)
            {
                var statusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.STATUS.ToString()];
                var fileStatus = ((int)LibraryFileStatusEnum.ACTIVE).ToString();
                var metaDataStatusValue = new MetaValuePair()
                {
                    Type = stringDataType,
                    Value = fileStatus
                };

                if (metaData.TryGetValue(statusKey, out _))
                {
                    metaData[statusKey] = metaDataStatusValue;
                }
                else
                {
                    metaData.Add(statusKey, metaDataStatusValue);
                }


                var approvedDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.APPROVED_DATE.ToString()];
                var approvedDateValue = new MetaValuePair()
                {
                    Type = stringDataType,
                    Value = currentTime.ToString("o", CultureInfo.InvariantCulture)
                };
                if (metaData.TryGetValue(approvedDateKey, out _))
                {
                    metaData[approvedDateKey] = approvedDateValue;
                }
                else
                {
                    metaData.Add(approvedDateKey, approvedDateValue);
                }

                SetReapproveDateToMetaData(metaData, currentTime);
            }

            if (artifact.MetaData == null) artifact.MetaData = metaData;

            return metaData;
        }

        private void SetReapproveDateToMetaData(IDictionary<string, MetaValuePair> metaData, DateTime currentDate)
        {
            var reapproveIntervalKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.REAPPROVE_INTERVAL.ToString()];
            var nextReapproveDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.NEXT_REAPPROVE_DATE.ToString()];
            var reapproveProcessStartKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.REAPPROVE_PROCESS_START.ToString()];
            var reapproveProcessStartDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.REAPPROVE_PROCESS_START_DATE.ToString()];
            
            if (metaData.TryGetValue(reapproveIntervalKey, out MetaValuePair reapproveInterval) && int.TryParse(reapproveInterval?.Value, out int interval))
            {
                var nextIntervalDate = currentDate.AddMonths(interval);
                var metaDataNextReapproveDateValue = new MetaValuePair()
                {
                    Type = "string",
                    Value = nextIntervalDate.ToString("o", CultureInfo.InvariantCulture)
                };

                if (metaData.TryGetValue(nextReapproveDateKey, out _))
                {
                    metaData[nextReapproveDateKey] = metaDataNextReapproveDateValue;
                }
                else
                {
                    metaData.Add(nextReapproveDateKey, metaDataNextReapproveDateValue);
                }

                if (
                    metaData.TryGetValue(reapproveProcessStartKey, out MetaValuePair value) &&
                    int.TryParse(value?.Value, out int processStart)
                )
                {
                    var processStartDate = nextIntervalDate.AddDays(-processStart);
                    if (currentDate >= processStartDate) processStartDate = currentDate;

                    var metaDataReapproveProcessStartDateValue = new MetaValuePair()
                    {
                        Type = "string",
                        Value = processStartDate.ToString("o", CultureInfo.InvariantCulture)
                    };

                    if (metaData.TryGetValue(reapproveProcessStartDateKey, out _))
                    {
                        metaData[reapproveProcessStartDateKey] = metaDataReapproveProcessStartDateValue;
                    }
                    else
                    {
                        metaData.Add(reapproveProcessStartDateKey, metaDataReapproveProcessStartDateValue);
                    }
                }
            }
        }

        private void PrepareObjectArtifactActivitySummaryUpdate(ObjectArtifact artifact, DateTime currentUtcTime)
        {
            if (artifact.ActivitySummary == null) artifact.ActivitySummary = new List<ActivitySummaryModel>();

            var praxisUserId = _objectArtifactUtilityService
                .GetPraxisUsersByUserIds(new[] { _securityContextProvider.GetSecurityContext().UserId })?.FirstOrDefault()?.ItemId;
            var performer = new ActivityPerformerModel()
            {
                PerformedBy = praxisUserId,
                PerformedOn = currentUtcTime
            };
            var reapprovalSummary = artifact.ActivitySummary.Find(s => s.ActivityName == ((int)ArtifactActivityName.APPROVAL).ToString());
            if (reapprovalSummary == null)
            {
                reapprovalSummary = new ActivitySummaryModel()
                {
                    ActivityName = ((int)ArtifactActivityName.APPROVAL).ToString(),
                    ActivityPerformerModel = new List<ActivityPerformerModel> { performer }
                };
                artifact.ActivitySummary.Add(reapprovalSummary);
            }
            else
            {
                reapprovalSummary.ActivityPerformerModel.Add(performer);
            }

        }

        private SearchResult GetObjectArtifactById(string id)
        {
            var objectArtifactSearchCommand = new ObjectArtifactSearchCommand()
            {
                ObjectArtifactId = id,
                Type = "approval-view"
            };
            var artifact = _objectArtifactSearchService.InitiateSearchObjectArtifact(objectArtifactSearchCommand);
            return artifact;
        }
    }
}