using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.PraxisOpenItem;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.PraxisConstants;
using GetCompletionListQuery = Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.GetCompletionListQuery;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisOpenItemService : IPraxisOpenItemService, IDeleteDataForClientInCollections
    {
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ILogger<PraxisOpenItemService> _logger;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ITaskManagementService _taskManagementService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IEmailDataBuilder _emailDataBuilder;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly IChangeLogService _changeLogService;
        private readonly ICirsOpenItemAttachmentService _cirsOpenItemAttachmentService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public PraxisOpenItemService(
            IMongoSecurityService mongoSecurityService,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider ecapRepository,
            IRepository repository,
            ILogger<PraxisOpenItemService> logger,
            ITaskManagementService taskManagementService,
            ICommonUtilService commonUtilService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IEmailDataBuilder emailDataBuilder,
            IEmailNotifierService emailNotifierService,
            IChangeLogService changeLogService,
            ICirsOpenItemAttachmentService cirsOpenItemAttachmentService,
            ICockpitSummaryCommandService cockpitSummaryCommandService
        )
        {
            _mongoSecurityService = mongoSecurityService;
            _securityContextProvider = securityContextProvider;
            _ecapRepository = ecapRepository;
            _repository = repository;
            _logger = logger;
            _taskManagementService = taskManagementService;
            _commonUtilService = commonUtilService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _emailDataBuilder = emailDataBuilder;
            _emailNotifierService = emailNotifierService;
            _changeLogService = changeLogService;
            _cirsOpenItemAttachmentService = cirsOpenItemAttachmentService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation("Going to delete {Item}, {Config} and {CompletionInfo} for client {ClientId}",
                nameof(PraxisOpenItem), nameof(PraxisOpenItemConfig), nameof(PraxisOpenItemCompletionInfo), clientId);

            try
            {
                var openItems = _repository.GetItems<PraxisOpenItem>(openItem => openItem.ClientId.Equals(clientId))
                    .ToList();
                if (!openItems.Any()) return;
                var openItemIds = openItems.Select(openItem => openItem.ItemId).ToList();
                var taskSummaryIds = openItems.Select(openItem => openItem.TaskSchedule.TaskSummaryId).ToList();
                var taskScheduleIds = openItems.Select(openItem => openItem.TaskSchedule.ItemId).ToList();

                var deleteTasks = new List<Task>
                {
                    _repository.DeleteAsync<PraxisOpenItem>(openItem => openItem.ClientId.Equals(clientId)),
                    _repository.DeleteAsync<PraxisOpenItemConfig>(openItemConfig => openItemConfig.ClientId.Equals(clientId)),
                    _repository.DeleteAsync<PraxisOpenItemCompletionInfo>(
                        completionInfo =>
                            openItemIds.Contains(completionInfo.PraxisOpenItemId)
                    ),
                    _repository.DeleteAsync<TaskSummary>(taskSummary => taskSummaryIds.Contains(taskSummary.ItemId)),
                    _repository.DeleteAsync<TaskSchedule>(taskSchedule => taskScheduleIds.Contains(taskSchedule.ItemId))
                };

                await Task.WhenAll(deleteTasks);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {Item}, {Config} and {CompletionInfo} for client {ClientId}. Error: {ErrorMessage}. Stacktrace: {StackTrace}",
                    nameof(PraxisOpenItem), nameof(PraxisOpenItemConfig), nameof(PraxisOpenItemCompletionInfo), clientId, e.Message, e.StackTrace);
            }
        }

        public void AddPraxisOpenItemConfigRowLevelSecurity(string itemId, string clientId)
        {
            var clientAdminAccessRole =
                _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
            var clientManagerAccessRole =
                _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);

            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);
            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);

            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisOpenItemConfig>(permission);
        }

        public void AddPraxisOpenItemRowLevelSecurity(string itemId, string clientId)
        {
            var clientAdminAccessRole =
                _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
            var clientManagerAccessRole =
                _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);

            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);
            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);
            permission.RolesAllowedToUpdate.Add(clientReadAccessRole);
            permission.RolesAllowedToDelete.Add(clientAdminAccessRole);

            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisOpenItem>(permission);
        }

        private List<string> GetPraxisUserIdsByClientId(string clientId)
        {
            return _repository.GetItems<PraxisUser>(pu => !pu.IsMarkedToDelete && pu.ClientList != null
                                            && pu.ClientList.Any(c => c.ClientId == clientId) && 
                                            !(pu.Roles != null && pu.Roles.Contains(RoleNames.GroupAdmin)))?.Select(p => p.ItemId)?.ToList();
        }

        public async Task<List<PraxisOpenItemResponseRecord>> GetPraxisOpenItems(
            string filter,
            string sort,
            int pageNumber,
            int pageSize
        )
        {
            var documents = await _commonUtilService
                .GetEntityQueryResponse<PraxisOpenItem>(filter, sort, "PraxisOpenItems", true, pageNumber, pageSize);
            var praxisOpenItems = documents.Results
                .Select(GetOpenItemRecord)
                .ToList();

            return praxisOpenItems;
        }

        private PraxisOpenItemResponseRecord GetOpenItemRecord(PraxisOpenItem p)
        {
            return new PraxisOpenItemResponseRecord
            {
                ActualBudget = p.ActualBudget,
                CategoryId = p.CategoryId,
                CategoryName = p.CategoryName,
                ClientId = p.ClientId,
                ControlledMembers = GetAggregatedControlledMembers(p),
                ControllingMembers = p.ControllingMembers,
                ControlledGroups = p.ControlledGroups,
                CreateDate = p.CreateDate,
                CreatedBy = p.CreatedBy,
                IsActive = p.IsActive,
                IsCompleted = p.IsCompleted,
                ItemId = p.ItemId,
                Language = p.Language,
                LastUpdateDate = p.LastUpdateDate,
                LastUpdatedBy = p.LastUpdatedBy,
                OpenItemConfigId = p.OpenItemConfigId,
                PlannedBudget = p.PlannedBudget,
                Remarks = p.Remarks,
                ResponseByAllMember = p.ResponseByAllMember,
                SubCategoryId = p.SubCategoryId,
                SubCategoryName = p.SubCategoryName,
                Tags = p.Tags,
                TaskDate = p.TaskDate,
                TaskReferenceId = p.TaskReferenceId,
                TaskReferenceTitle = p.TaskReferenceTitle,
                Title = p.Title,
                IsOverDueMailSent = p.IsOverDueMailSent,
                DocumentInfo = p.DocumentInfo,
                LastCompletionStatus = p.LastCompletionStatus,
                OverAllCompletionStatus = p.OverAllCompletionStatus,
                TaskReference = p.TaskReference,
                TaskSchedule = p.TaskSchedule,
                Topic = p.Topic
            };
        }

        public IEnumerable<string> GetAggregatedControlledMembers(PraxisOpenItem p)
        {
            p.ControlledMembers ??= new List<string>();
            var controlledMembers = (!p.ControlledMembers.Any()
                ? GetPraxisUserIdsByClientId(p.ClientId)
                : p.ControlledMembers) ?? new List<string>();
            var controlledGroupMembers = GetControlledMembersWithUserGroup(p.ControlledGroups, p.ClientId) ?? new List<string>();

            return p.ControlledMembers.Any() switch
            {
                false when !controlledGroupMembers.Any() => controlledMembers,
                false => controlledGroupMembers,
                _ => !controlledGroupMembers.Any()
                    ? controlledMembers
                    : controlledMembers.Concat(controlledGroupMembers).Distinct().ToList()
            };
        }

        private List<string> GetControlledMembersWithUserGroup(IEnumerable<string> controlledGroups, string clientId)
        {
            controlledGroups ??= new List<string>();
            var controlledMembers = _repository.GetItems<PraxisUser>(pu =>
                pu.ClientList != null &&
                !(pu.Roles != null && pu.Roles.Contains(RoleNames.GroupAdmin)) &&
                pu.ClientList.Any(c => c.ClientId == clientId && c.Roles.Any(r => controlledGroups.Contains(r)))
            )?
            .Select(user => user.ItemId)
            .ToList() ?? new List<string>();

            controlledMembers = controlledMembers.Distinct().ToList();

            return controlledMembers;
        }

        public PraxisOpenItem GetPraxisPraxisOpenItem(string itemId)
        {
            throw new NotImplementedException();
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }

        public void UpdatePraxisOpenItemConfig(string itemId)
        {
            throw new NotImplementedException();
        }

        public List<PraxisOpenItem> GetAllPraxisOpenItem()
        {
            throw new NotImplementedException();
        }

        public List<PraxisOpenItemConfig> GetAllPraxisOpenItemConfig()
        {
            throw new NotImplementedException();
        }

        public PraxisOpenItemConfig GetPraxisOpenItemConfig(string itemId)
        {
            throw new NotImplementedException();
        }

        public List<string> GetNotCompletedMembers(string ItemId, List<string> controlledMembers)
        {
            var notCompletedMembers = new List<string>();
            foreach (var controlledMember in controlledMembers)
            {
                var openItemCompletionInfo = _repository.GetItem<PraxisOpenItemCompletionInfo>(
                    o =>
                        o.ReportedByUserId == controlledMember && o.PraxisOpenItemId == ItemId
                );
                if (openItemCompletionInfo == null)
                {
                    var person = _repository.GetItem<Person>(o => o.ItemId.Equals(controlledMember));
                    notCompletedMembers.Add(person.DisplayName);
                }
            }

            return notCompletedMembers;
        }

        public Task<QueryCompletionResponse> GetOpenItemCompletionDetails(GetCompletionListQuery getCompletion)
        {
            var queryCompletionResponse = new QueryCompletionResponse();
            try
            {
                var praxisOpenItems = string.IsNullOrEmpty(getCompletion.ItemId) switch
                {
                    false when getCompletion.TaskReferenceId != null => _repository.GetItems<PraxisOpenItem>(
                            openItem =>
                                openItem.ItemId == getCompletion.ItemId &&
                                openItem.TaskReferenceId == getCompletion.TaskReferenceId &&
                                !openItem.IsMarkedToDelete
                        )
                        .ToList(),
                    false => _repository.GetItems<PraxisOpenItem>(
                            openItem =>
                                openItem.ItemId == getCompletion.ItemId &&
                                !openItem.IsMarkedToDelete
                        )
                        .ToList(),
                    _ => _repository.GetItems<PraxisOpenItem>(
                            openItem =>
                                openItem.TaskReferenceId == getCompletion.TaskReferenceId &&
                                !openItem.IsMarkedToDelete &&
                                openItem.IsActive
                        )
                        .ToList()
                };

                var (measuresTakenPendingInfo, completionHistory) = GetMeasuresTakenPendingInfo(praxisOpenItems);

                queryCompletionResponse.AssignedMembers = praxisOpenItems
                    .SelectMany(GetAggregatedControlledMembers)
                    .ToList();
                queryCompletionResponse.PendingMembers = measuresTakenPendingInfo.PendingMembers;
                queryCompletionResponse.DoneMembers = measuresTakenPendingInfo.DoneMembers;
                queryCompletionResponse.AlternativelyDoneMembers = measuresTakenPendingInfo.AlternativelyDoneMembers;
                queryCompletionResponse.CompletionHistory = completionHistory;

                if (!string.IsNullOrEmpty(getCompletion.TaskReferenceId))
                {
                    if (!praxisOpenItems.Any())
                    {
                        queryCompletionResponse.MeasuresForReference = new ReferenceMeasure();
                    }
                    else
                    {
                        queryCompletionResponse.MeasuresForReference = GetMeasuresForReferenceInfo(completionHistory);

                        if (praxisOpenItems.ElementAt(0).TaskReference.Key == "risk-management")
                        {
                            UpdateMeasuresTakenPendingCount(
                                queryCompletionResponse.MeasuresForReference.MeasuresTaken,
                                queryCompletionResponse.MeasuresForReference.MeasuresPending,
                                getCompletion.TaskReferenceId
                            );
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occurred while generating Open Item Completion Details");
                _logger.LogError("Message: {ErrorMessage} StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }

            return Task.FromResult(queryCompletionResponse);
        }

        public async Task UpdateOpenItemLibraryFormResponse(ObjectArtifact artifact)
        {
            try
            {
                if (artifact == null) return;
                if (!string.IsNullOrEmpty(artifact.OwnerId))
                {
                    var praxisUser = await _repository.GetItemAsync<PraxisUser>(pu => pu.UserId == artifact.OwnerId);
                    if (praxisUser != null && artifact.MetaData != null)
                    {
                        var metaData = artifact.MetaData;
                        var praxisUserId = praxisUser.ItemId;
                        var entityName = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, "EntityName");
                        var entityId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, "EntityId");
                        var isComplete = _objectArtifactUtilityService.IsACompletedFormResponse(metaData);


                        var originalFormId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                                                $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"]);

                        if (entityName == EntityName.PraxisOpenItem && !string.IsNullOrEmpty(entityId))
                        {
                            var openItemAnswer = _repository.GetItem<PraxisOpenItemCompletionInfo>
                                            (p => p.PraxisOpenItemId == entityId && p.ReportedByUserId == praxisUserId && p.LibraryFormResponses != null &&
                                    p.LibraryFormResponses.Any(l => l.OriginalFormId == originalFormId && !l.IsComplete));

                            if (openItemAnswer != null)
                            {
                                var libraryFormResponse = openItemAnswer.LibraryFormResponses
                                            .FirstOrDefault(l => l.OriginalFormId == originalFormId);
                                if (libraryFormResponse != null)
                                {
                                    libraryFormResponse.LibraryFormId = artifact.ItemId;
                                    libraryFormResponse.CompletedBy = praxisUserId;
                                    if (isComplete)
                                    {
                                        libraryFormResponse.IsComplete = isComplete;
                                        libraryFormResponse.CompletedOn = DateTime.UtcNow;

                                        UpdateOpenItemCompletionData(openItemAnswer);
                                    }
                                    await _repository.UpdateAsync(p => p.ItemId == openItemAnswer.ItemId, openItemAnswer);

                                    //if (isComplete)
                                    //{
                                    //    var openItem = _repository.GetItem<PraxisOpenItem>(o => o.ItemId == entityId);
                                    //    if (openItem != null)
                                    //    {
                                    //        UpdatePraxisOpenItemCompletionStatus(
                                    //            openItem, openItemAnswer, false
                                    //        );
                                    //        ProcessEmailForAssignedUsersCompletion(openItem, openItemAnswer);
                                    //        await _cockpitSummaryCommandService.CreateSummary(openItem.ItemId, EntityName.PraxisOpenItem, true);
                                    //    }
                                    //}
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in UpdateOpenItemLibraryFormResponse: {ErrorMessage}", ex.Message);
            }
        }

        private void UpdateOpenItemCompletionData(PraxisOpenItemCompletionInfo completionInfo)
        {
            completionInfo.Comment = "";
            completionInfo.Completion = new PraxisKeyValue()
            {
                Key = "library-form-submitted",
                Value = "LibraryFormSubmitted"
            };
            completionInfo.ReportedTime = DateTime.UtcNow;
            completionInfo.Language = "en-US";

        }

        private ReferenceMeasure GetMeasuresForReferenceInfo(IEnumerable<CompletionHistory> completionHistories)
        {
            var measuresForReference = new ReferenceMeasure
            {
                MeasuresPending = 0,
                MeasuresTaken = 0
            };
            foreach (var completionHistory in completionHistories)
            {
                if (completionHistory.IsSingleAnswerTask)
                {
                    if (completionHistory.IsCompleted)
                    {
                        measuresForReference.MeasuresTaken++;
                    }
                    else
                    {
                        measuresForReference.MeasuresPending++;
                    }
                }
                else
                {
                    var pendingTasks = completionHistory.CompletionStatus.Where(
                        status => status.Status.ToLower() == "pending"
                    );
                    measuresForReference.MeasuresPending += pendingTasks.Count();
                    var completeTasks = completionHistory.CompletionStatus.Where(
                        status => status.Status.ToLower() != "pending"
                    );
                    measuresForReference.MeasuresTaken += completeTasks.Count();
                }
            }

            return measuresForReference;
        }

        public async Task<EntityQueryResponse<PraxisOpenItem>> GetPraxisOpenReportData(string filter, string sort)
        {
            return await Task.Run(
                () =>
                {
                    FilterDefinition<BsonDocument> queryFilter = new BsonDocument();

                    if (!string.IsNullOrEmpty(filter)) queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);

                    var securityContext = _securityContextProvider.GetSecurityContext();

                    queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                        PdsActionEnum.Read,
                        securityContext,
                        securityContext.Roles.ToList()
                    );

                    long totalRecord = 0;

                    var collections = _ecapRepository
                        .GetTenantDataContext()
                        .GetCollection<BsonDocument>("PraxisOpenItems")
                        .Aggregate()
                        .Match(queryFilter);

                    totalRecord = collections.ToEnumerable().Count();

                    if (!string.IsNullOrEmpty(sort)) collections = collections.Sort(BsonDocument.Parse(sort));

                    var results = collections.ToEnumerable()
                        .Select(document => BsonSerializer.Deserialize<PraxisOpenItem>(document));

                    return new EntityQueryResponse<PraxisOpenItem>
                    {
                        Results = results.ToList(),
                        TotalRecordCount = totalRecord
                    };
                }
            );
        }

        public void UpdatePraxisOpenItemCompletionStatus(
            PraxisOpenItem praxisOpenItem,
            PraxisOpenItemCompletionInfo praxisOpenItemCompletionInfo,
            bool isUpdate
        )
        {
            if (praxisOpenItem.IsCompleted && praxisOpenItem.OverAllCompletionStatus.Equals(OpenItemDoneStatus))
            {
                if (praxisOpenItem.TaskReference?.Key?.Equals("reporting", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    _cirsOpenItemAttachmentService.UpdateCirsOpenItemCompletionStatus(praxisOpenItem.ItemId, praxisOpenItem.OverAllCompletionStatus).GetAwaiter().GetResult();
                }
                return;
            }


            if (praxisOpenItem.ResponseByAllMember ?? false)
            {
                var completionResponse = GetOpenItemCompletionDetails(
                    new GetCompletionListQuery { ItemId = praxisOpenItem.ItemId }
                ).GetAwaiter().GetResult();

                if (completionResponse.PendingMembers.Any())
                {
                    _logger.LogInformation(
                        $"ToDo with title {praxisOpenItem.Title} and itemId {praxisOpenItem.ItemId} is not yet complete"
                    );
                }
                else
                {
                    var completionStatus = completionResponse.AlternativelyDoneMembers.Any()
                        ? OpenItemAlternativelyDoneStatus
                        : OpenItemDoneStatus;
                    MarkPraxisOpenItemAsComplete(praxisOpenItem, praxisOpenItemCompletionInfo, completionStatus).GetAwaiter().GetResult();


                    if (praxisOpenItem.TaskReference?.Key?.Equals("reporting", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        _cirsOpenItemAttachmentService.UpdateCirsOpenItemCompletionStatus(praxisOpenItem.ItemId, completionStatus).GetAwaiter().GetResult();
                    }
                }
            }
            else if (praxisOpenItemCompletionInfo.Completion != null && praxisOpenItemCompletionInfo.Completion.Key != OpenItemPendingStatus.Key)
            {
                MarkPraxisOpenItemAsComplete(
                    praxisOpenItem,
                    praxisOpenItemCompletionInfo,
                    praxisOpenItemCompletionInfo.Completion
                ).GetAwaiter().GetResult();


                if (praxisOpenItem.TaskReference?.Key?.Equals("reporting", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    _cirsOpenItemAttachmentService.UpdateCirsOpenItemCompletionStatus(praxisOpenItem.ItemId, praxisOpenItemCompletionInfo.Completion).GetAwaiter().GetResult();
                }
            }
        }

        private async Task MarkPraxisOpenItemAsComplete(
            PraxisOpenItem praxisOpenItem,
            PraxisOpenItemCompletionInfo praxisOpenItemCompletionInfo,
            PraxisKeyValue completionStatus
        )
        {
            _logger.LogInformation("Marking to do with title {Title} and itemId {ItemId} as Complete", praxisOpenItem.Title, praxisOpenItem.ItemId);

            praxisOpenItem.ActualBudget = praxisOpenItemCompletionInfo.ActualBudget;
            praxisOpenItem.IsCompleted = true;
            praxisOpenItem.OverAllCompletionStatus = completionStatus;
            await _repository.UpdateAsync(item => item.ItemId.Equals(praxisOpenItem.ItemId), praxisOpenItem);
            await UpdateTaskForToDo(
                new
                {
                    TaskScheduleId = praxisOpenItem.TaskSchedule.ItemId,
                    RelatedEntityObject = new
                    {
                        IsCompleted = true,
                        OverAllCompletionStatus = completionStatus,
                        praxisOpenItemCompletionInfo.ActualBudget
                    },
                    HasTaskScheduleIntoRelatedEntity = true
                }
            );
        }

        public async Task UpdateTaskForToDo(dynamic payload)
        {
            await _taskManagementService.UpdateTask(
                payload
            );
        }

        public async Task PopulateOverAllCompletionStatus(string filterString = "{}")
        {
            var openItems = (await _commonUtilService.GetEntityQueryResponse<PraxisOpenItem>(filterString)).Results
                .Where(openItem => openItem.OverAllCompletionStatus == null)
                .ToList();
            _logger.LogInformation("Total open items: {Count}", openItems.Count);
            for (var index = 0; index < openItems.Count; index++)
            {
                var openItem = openItems[index];
                try
                {
                    _logger.LogInformation(
                        $"Fixing: {openItem.Title}({index})({openItem.ItemId})"
                    );
                    var completionInfos =
                        (await _commonUtilService.GetEntityQueryResponse<PraxisOpenItemCompletionInfo>(
                            "{PraxisOpenItemId: \"" + openItem.ItemId + "\"}",
                            "{LastModifiedDate: -1}"
                        )).Results;
                    if (openItem.IsCompleted)
                    {
                        var lastCompletionStatus = completionInfos.ElementAt(0) ?? new PraxisOpenItemCompletionInfo();
                        await MarkPraxisOpenItemAsComplete(openItem, lastCompletionStatus, OpenItemDoneStatus);
                    }
                    else
                    {
                        if (openItem.ResponseByAllMember ?? false)
                        {
                            var completionResponse = await GetOpenItemCompletionDetails(
                                new GetCompletionListQuery { ItemId = openItem.ItemId }
                            );
                            if (completionResponse.PendingMembers.Any())
                                await SetPendingStatus(openItem);
                            else if (completionResponse.AlternativelyDoneMembers.Any())
                                await MarkPraxisOpenItemAsComplete(openItem, completionInfos.ElementAt(0), OpenItemDoneStatus);
                            else
                            {
                                if (!completionResponse.DoneMembers.Any())
                                    await SetPendingStatus(openItem);
                            }
                        }
                        else
                        {
                            if (completionInfos.Any())
                            {
                                await MarkPraxisOpenItemAsComplete(
                                    openItem,
                                    completionInfos.ElementAt(0),
                                    OpenItemDoneStatus
                                );
                            }
                            else
                            {
                                await SetPendingStatus(openItem);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task SetPendingStatus(PraxisOpenItem openItem)
        {
            openItem.OverAllCompletionStatus = OpenItemPendingStatus;
            await _repository.UpdateAsync(item => item.ItemId.Equals(openItem.ItemId), openItem);
            await _taskManagementService.UpdateTask(
                new
                {
                    TaskScheduleId = openItem.TaskSchedule.ItemId,
                    RelatedEntityObject = new { OverAllCompletionStatus = OpenItemPendingStatus },
                    HasTaskScheduleIntoRelatedEntity = true
                }
            );
        }

        private (QueryCompletionResponse queryCompletionResponse, List<CompletionHistory> completionHistories)
            GetMeasuresTakenPendingInfo(List<PraxisOpenItem> praxisOpenItems)
        {
            var queryCompletionResponse = new QueryCompletionResponse();
            var notCompletedMembers = new List<string>();
            var doneMembers = new List<string>();
            var alternativelyMembers = new List<string>();
            var completionHistories = new List<CompletionHistory>();
            for (var index = 0; index < praxisOpenItems.Count; index++)
            {
                var openItem = praxisOpenItems[index];
                _logger.LogInformation("Generating MeasuresTakenPendingInfo for open item with ItemId: {ItemId}({Index})", openItem.ItemId, index);
                var controlledMembers = GetAggregatedControlledMembers(openItem).ToList();
                var openItemCompletionInfoList = GetPraxisOpenItemCompletionInfos(openItem.ItemId, controlledMembers);

                if (openItem.ResponseByAllMember ?? false)
                {

                    if (openItemCompletionInfoList.Any())
                    {
                        GetDoneAndAlternativelyDoneMembers(openItemCompletionInfoList, doneMembers, alternativelyMembers);

                        notCompletedMembers.AddRange(
                            controlledMembers.Where(
                                praxisUserId =>
                                    !doneMembers.Contains(praxisUserId) &&
                                    !alternativelyMembers.Contains(praxisUserId)
                            )
                        );
                    }
                    else
                    {
                        notCompletedMembers.AddRange(controlledMembers.ToList());
                    }
                }
                else
                {
                    if (openItemCompletionInfoList.Any())
                    {
                        GetDoneAndAlternativelyDoneMembers(openItemCompletionInfoList, doneMembers, alternativelyMembers);

                        if (openItemCompletionInfoList.Count != controlledMembers.Count)
                        {
                            var completedIds = openItemCompletionInfoList.Select(o => o.ReportedByUserId).ToList();
                            var assignedIds = controlledMembers.ToList();
                            var notCompletedIds = assignedIds.Except(completedIds).ToList();
                            if (notCompletedIds.Any()) notCompletedMembers.AddRange(notCompletedIds);
                        }
                    }
                    else
                    {
                        notCompletedMembers.AddRange(controlledMembers.ToList());
                    }
                }

                _logger.LogInformation("Completed generating MeasuresTakenPendingInfo for open item with ItemId: {ItemId}", openItem.ItemId);

                completionHistories.Add(GetSingleOpenItemCompletionHistory(openItem, controlledMembers));
            }

            queryCompletionResponse.PendingMembers = notCompletedMembers;
            queryCompletionResponse.DoneMembers = doneMembers;
            queryCompletionResponse.AlternativelyDoneMembers = alternativelyMembers;
            _logger.LogInformation("Generated MeasuresTakenPendingInfo for {Count} open items", praxisOpenItems.Count);
            return (queryCompletionResponse, completionHistories);
        }

        private CompletionHistory GetSingleOpenItemCompletionHistory(PraxisOpenItem openItem, List<string> controlledMembers)
        {
            _logger.LogInformation("Generating CompletionHistory for open item with ItemId {ItemId}", openItem.ItemId);
            var completionHistory = new CompletionHistory
            {
                TaskId = openItem.ItemId,
                TaskTitle = openItem.Title,
                IsCompleted = openItem.IsCompleted,
                IsSingleAnswerTask = !(openItem.ResponseByAllMember ?? false),
            };

            if (openItem.TaskSchedule?.TaskDateTime != null)
            {
                completionHistory.DueDate = openItem.TaskSchedule!.TaskDateTime;
            }

            var completionStatusList = new List<CompletionStatus>();
            var openItemCompletionInfos = GetPraxisOpenItemCompletionInfos(openItem.ItemId, controlledMembers);

            if (completionHistory.IsSingleAnswerTask)
            {
                var praxisUsers = _repository.GetItems<PraxisUser>(
                        praxisUser => controlledMembers.Contains(praxisUser.ItemId) &&
                                      !praxisUser.IsMarkedToDelete
                    )
                    .ToList();

                List<PraxisUser> relatedUsers;
                string status;
                if (completionHistory.IsCompleted)
                {
                    relatedUsers = openItemCompletionInfos
                        .AsEnumerable()
                        .Select(completionInfo =>
                            praxisUsers.Find(pu => pu.ItemId.Equals(completionInfo.ReportedByUserId)))
                        .ToList();
                    status = OpenItemDoneStatus.Key;
                }
                else
                {
                    relatedUsers = praxisUsers;
                    status = OpenItemPendingStatus.Key;
                }

                var relatedUserNames = string.Join(", ", relatedUsers.Select(pu => pu.DisplayName));
                completionStatusList.Add(new CompletionStatus { AssignMember = relatedUserNames, Status = status });

                completionHistory.CompletionStatus = completionStatusList;
            }
            else
            {
                if (openItemCompletionInfos.Any())
                {
                    foreach (var openItemCompletionInfo in openItemCompletionInfos)
                    {
                        var praxisUser = _repository.GetItem<PraxisUser>(
                            p => p.ItemId == openItemCompletionInfo.ReportedByUserId && !p.IsMarkedToDelete
                        );

                        completionStatusList.Add(
                            new CompletionStatus
                            {
                                AssignMember = praxisUser?.DisplayName,
                                Status = openItemCompletionInfo.Completion.Key
                            }
                        );
                    }

                    if (openItemCompletionInfos.Count != controlledMembers.Count)
                    {
                        var answeredPersons = openItemCompletionInfos.Select(a => a.ReportedByUserId).ToList();
                        var pendingAnsPersons =
                            controlledMembers.Where(c => !answeredPersons.Contains(c));

                        foreach (var pendingAnsPerson in pendingAnsPersons)
                        {
                            var praxisUser = _repository.GetItem<PraxisUser>(
                                p => p.ItemId == pendingAnsPerson && !p.IsMarkedToDelete
                            );
                            completionStatusList.Add(
                                new CompletionStatus
                                {
                                    AssignMember = praxisUser?.DisplayName,
                                    Status = OpenItemPendingStatus.Key
                                }
                            );
                        }
                    }

                    completionHistory.CompletionStatus = completionStatusList;
                }
                else
                {
                    var personList = _repository.GetItems<PraxisUser>(
                            p =>
                                controlledMembers.Contains(p.ItemId) && !p.IsMarkedToDelete
                        )
                        .ToList();
                    completionStatusList.AddRange(
                        personList.Select(
                            person => new CompletionStatus
                            {
                                AssignMember = person?.DisplayName,
                                Status = OpenItemPendingStatus.Key
                            }
                        )
                    );
                    completionHistory.CompletionStatus = completionStatusList;
                }
            }

            return completionHistory;
        }

        private void UpdateMeasuresTakenPendingCount(int measuresTaken, int measuresPending, string riskItemId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "MeasuresTaken", measuresTaken },
                    { "MeasuresPending", measuresPending },
                    { "LastUpdateDate", DateTime.UtcNow.ToLocalTime() }
                };

                _repository.UpdateAsync<PraxisRisk>(r => r.ItemId == riskItemId, updates).Wait();
                _logger.LogInformation("Data has been successfully updated to {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisRisk), riskItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during updating {EntityName} entity data with ItemId: {ItemId} Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    nameof(PraxisRisk), riskItemId, ex.Message, ex.StackTrace);
            }
        }

        public void ProcessEmailForAssignedUsersCompletion(PraxisOpenItem praxisOpenItem,
            PraxisOpenItemCompletionInfo praxisOpenItemCompletionInfo)
        {
            if (!string.IsNullOrEmpty(praxisOpenItem.ItemId) && praxisOpenItem.CreatedBy != null)
            {
                var praxisUserIds = GetAggregatedControlledMembers(praxisOpenItem).ToList();
                if (praxisUserIds.Count > 0)
                {
                    SendEmailToPersons(praxisUserIds.Distinct().ToList(), praxisOpenItem, praxisOpenItemCompletionInfo);
                }
            }
        }

        private void SendEmailToPersons(List<string> praxisUserIds, PraxisOpenItem praxisOpenItem,
            PraxisOpenItemCompletionInfo praxisOpenItemCompletionInfo)
        {
            var clientName = string.Empty;
            var client = _repository.GetItem<PraxisClient>(c => c.ItemId == praxisOpenItem.ClientId);
            if (client != null)
            {
                clientName = client.ClientName;
            }

            foreach (var praxisUserId in praxisUserIds)
            {
                var person = _repository.GetItem<Person>(p => p.ItemId.Equals(praxisUserId) && !p.IsMarkedToDelete);
                string completedBy = praxisOpenItemCompletionInfo.ReportedByUserId;
                var completedByPerson =
                    _repository.GetItem<Person>(p => p.ItemId.Equals(completedBy) && !p.IsMarkedToDelete);

                if (string.IsNullOrWhiteSpace(person?.Email) || completedByPerson == null) continue;
                string assignedBy = string.Empty;
                if (praxisOpenItem != null)
                {
                    var user = _repository.GetItem<PraxisUser>(c => c.ItemId == praxisOpenItem.CreatedBy);
                    assignedBy = user?.DisplayName ?? string.Empty;
                }
                var emailData = _emailDataBuilder.BuildOpenItemEmailData(praxisOpenItem, person, completedByPerson, clientName, assignedBy);
                _emailNotifierService.SendOpenItemAssignedEmail(person, emailData);
            }
        }

        private List<PraxisOpenItemCompletionInfo> GetPraxisOpenItemCompletionInfos(string itemId,
            List<string> controlledMembers)
        {
            var openItemCompletionInfoList = _repository.GetItems<PraxisOpenItemCompletionInfo>(
                    completionInfo =>
                        controlledMembers.Contains(completionInfo.ReportedByUserId) &&
                        completionInfo.PraxisOpenItemId == itemId &&
                        completionInfo.Completion != null &&
                        !completionInfo.IsMarkedToDelete
                )
                .ToList();
            return openItemCompletionInfoList;
        }

        private void GetDoneAndAlternativelyDoneMembers(
            List<PraxisOpenItemCompletionInfo> openItemCompletionInfoList,
            List<string> doneMembers,
            List<string> alternativelyMembers)
        {
            foreach (var openItemCompletionInfo in openItemCompletionInfoList)
            {
                if (openItemCompletionInfo.Completion.Key == OpenItemDoneStatus.Key)
                    doneMembers.Add(openItemCompletionInfo.ReportedByUserId);
                else if (openItemCompletionInfo.Completion.Key == OpenItemAlternativelyDoneStatus.Key)
                    alternativelyMembers.Add(openItemCompletionInfo.ReportedByUserId);
            }
        }
        public async Task<long> GetOpenItemDocumentCount(string filter)
        {
            var result = await _commonUtilService.GetEntityQueryResponse<PraxisOpenItem>(filter);
            return result.TotalRecordCount;
        }

        public async Task<List<PraxisOpenItem>> GetProjectedOpenItemsWithSpecificTraining(string trainingId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var praxisCurrentUser = GetPraxisUserByUserId(securityContext.UserId);
            var builder = Builders<PraxisOpenItem>.Filter;

            var controlledMembersFilter = builder.Or(
                builder.Exists(t => t.ControlledMembers, exists: false),
                builder.Size(t => t.ControlledMembers, 0),
                builder.AnyEq(t => t.ControlledMembers, praxisCurrentUser.ItemId)
            );

            var controlledGroupsFilter = builder.And(
                builder.Exists(t => t.ControlledGroups),
                builder.AnyIn(t => t.ControlledGroups, securityContext.Roles)
            );

            var filter = builder.And(
                builder.Exists(t => t.TaskReference),
                builder.Exists(t => t.TaskReferenceId),
                builder.Eq(t => t.TaskReference.Value, "Training"),
                builder.Eq(t => t.TaskReferenceId, trainingId),
                builder.Or(controlledMembersFilter, controlledGroupsFilter)
            );
            var projection = Builders<PraxisOpenItem>.Projection.Include(t => t.ItemId);
            var client = _ecapRepository
                .GetTenantDataContext()
                .GetCollection<PraxisOpenItem>($"{nameof(PraxisOpenItem)}s");
            var openItems = await client
                .Find(filter)
                .Project<PraxisOpenItem>(projection)
                .ToListAsync();
            return openItems ?? new List<PraxisOpenItem>();
        }
        private PraxisUser GetPraxisUserByUserId(string userId)
        {
            return _repository.GetItem<PraxisUser>(pu => pu.UserId == userId);
        }
    }
}