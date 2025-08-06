#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Common;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisEquipmentMaintenanceService : IPraxisEquipmentMaintenanceService
    {
        private readonly ILogger<PraxisEquipmentMaintenanceService> _logger;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly IRepository _repository;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly string _taskManagementServiceBaseUrl;
        private readonly IServiceClient _serviceClient;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly IEmailDataBuilder _emailDataBuilder;
        private readonly IAuthUtilityService _authUtilityService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IPraxisFileService _praxisFileService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;
        private readonly DeleteTaskScheduleDataForPraxisProcessGuide _deleteTaskScheduleDataForPraxisProcessGuide;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;

        public PraxisEquipmentMaintenanceService(
            ILogger<PraxisEquipmentMaintenanceService> logger,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider ecapRepository,
            IMongoSecurityService mongoSecurityService,
            IRepository repository,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IConfiguration configuration,
            IServiceClient serviceClient,
            IEmailNotifierService emailNotifierService,
            IEmailDataBuilder emailDataBuilder,
            IAuthUtilityService authUtilityService,
            ICommonUtilService commonUtilService,
            IPraxisFileService praxisFileService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IGenericEventPublishService genericEventPublishService,
            ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService,
            DeleteTaskScheduleDataForPraxisProcessGuide deleteTaskScheduleDataForPraxisProcessGuide,
            IPraxisReportTemplateService praxisReportTemplateService
        )
        {
            _logger = logger;
            this._securityContextProvider = securityContextProvider;
            this._ecapRepository = ecapRepository;
            this._mongoSecurityService = mongoSecurityService;
            _repository = repository;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _taskManagementServiceBaseUrl = configuration["TaskManagementServiceBaseUrl"];
            _serviceClient = serviceClient;
            _emailDataBuilder = emailDataBuilder;
            _emailNotifierService = emailNotifierService;
            _authUtilityService = authUtilityService;
            _commonUtilService = commonUtilService;
            _praxisFileService = praxisFileService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _genericEventPublishService = genericEventPublishService;
            _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
            _deleteTaskScheduleDataForPraxisProcessGuide = deleteTaskScheduleDataForPraxisProcessGuide;
            _praxisReportTemplateService = praxisReportTemplateService;
        }
        public void AddRowLevelSecurity(string itemId, string clientId)
        {
            var clientAdminAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientManagerAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);


            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };
            permission.RolesAllowedToUpdate.Add(clientReadAccessRole);
            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);
            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);
            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);

            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisEquipmentMaintenance>(permission);
        }

        public List<PraxisEquipmentMaintenance> GetAllPraxisEquipmentMaintenance()
        {
            throw new NotImplementedException();
        }

        public PraxisEquipmentMaintenance GetPraxisEquipmentMaintenance(string itemId)
        {
            throw new NotImplementedException();
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }

        public void UpdatePraxisEquipmentMaintenance(string itemId)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<PraxisEquipmentMaintenance>> GetPraxisMaintenances(string filter, string sort, int pageNumber, int pageSize)
        {
            return await Task.Run(() =>
            {
                FilterDefinition<BsonDocument> queryFilter = new BsonDocument();

                if (!string.IsNullOrEmpty(filter))
                {
                    queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);
                }

                var securityContext = _securityContextProvider.GetSecurityContext();

                queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                    PdsActionEnum.Read,
                    securityContext,
                    securityContext.Roles.ToList()
                );

                long totalRecord = 0;

                pageNumber += 1;
                var skip = pageSize * (pageNumber - 1);

                var collections = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>($"PraxisEquipmentMaintenances")
                    .Aggregate()
                    .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();

                if (!string.IsNullOrEmpty(sort))
                {
                    collections = collections.Sort(BsonDocument.Parse(sort));
                }

                collections = collections.Skip(skip).Limit(pageSize);

                var results = collections.ToEnumerable()
                    .Select(document => BsonSerializer.Deserialize<PraxisEquipmentMaintenance>(document));

                return new EntityQueryResponse<PraxisEquipmentMaintenance>
                {
                    Results = results.ToList(),
                    TotalRecordCount = totalRecord
                };

            });
        }

        public async Task UpdateEquipmentMaintenanceLibraryFormResponse(ObjectArtifact artifact)
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

                        if (entityName == EntityName.PraxisEquipmentMaintenance && !string.IsNullOrEmpty(entityId))
                        {
                            var equipmentMaintenance = _repository.GetItem<PraxisEquipmentMaintenance>
                                            (p => p.ItemId == entityId && !p.IsMarkedToDelete);

                            if (equipmentMaintenance != null)
                            {
                                var libraryFormResponse = equipmentMaintenance?.LibraryFormResponses?
                                                .Find(l => l.OriginalFormId == originalFormId && l.CompletedBy == praxisUserId);
                                if (libraryFormResponse != null)
                                {
                                    libraryFormResponse.LibraryFormId = artifact.ItemId;
                                    libraryFormResponse.CompletedBy = praxisUserId;
                                    if (isComplete)
                                    {
                                        libraryFormResponse.IsComplete = isComplete;
                                        libraryFormResponse.CompletedOn = DateTime.UtcNow;
                                    }
                                }
                                else
                                {
                                    libraryFormResponse = new PraxisLibraryFormResponse()
                                    {
                                        OriginalFormId = originalFormId,
                                        LibraryFormId = artifact.ItemId,
                                        CompletedBy = praxisUserId
                                    };
                                    if (isComplete)
                                    {
                                        libraryFormResponse.IsComplete = isComplete;
                                        libraryFormResponse.CompletedOn = DateTime.UtcNow;
                                    }
                                    var responses = equipmentMaintenance?.LibraryFormResponses?.ToList() ?? new List<PraxisLibraryFormResponse>();
                                    responses.Add(libraryFormResponse);
                                    equipmentMaintenance.LibraryFormResponses = responses;
                                }
                                await _repository.UpdateAsync(p => p.ItemId == equipmentMaintenance.ItemId, equipmentMaintenance);
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in UpdateEquipmentMaintenanceLibraryFormResponse: {ErrorMessage}", ex.Message);
            }
        }

        public async Task<Dictionary<string, object>> GetEquipmentMaintenanceForExternalUser(GetEquipmentMaintenanceForExternalUserQuery query)
        {
            try
            {
                var dictionary = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(query?.EquipmentId))
                {
                    var maintenances = _repository.GetItems<PraxisEquipmentMaintenance>
                        (eq => eq.PraxisEquipmentId == query.EquipmentId && !eq.IsMarkedToDelete)?
                        .OrderBy(m => m.MaintenanceEndDate)?.ToList();
                    dictionary.Add(EntityName.PraxisEquipmentMaintenance + 's', maintenances);
                    GetPraxisUsersFromMaintenance(dictionary, maintenances);
                }
                else if (!string.IsNullOrEmpty(query?.EquipmentMaintenanceId))
                {
                    var maintenance = await _repository.GetItemAsync<PraxisEquipmentMaintenance>(eq => eq.ItemId == query.EquipmentMaintenanceId && !eq.IsMarkedToDelete);
                    dictionary.Add(EntityName.PraxisEquipmentMaintenance, maintenance);
                    GetPraxisUsersFromMaintenance(dictionary, new List<PraxisEquipmentMaintenance> { maintenance });
                    GetObjectArtifactsFromMaintenance(dictionary, new List<PraxisEquipmentMaintenance> { maintenance });
                }
                return dictionary;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in GetEquipmentMaintenanceForExternalUser: {ex.Message}");
            }
            return null;
        }

        public async Task<Dictionary<string, object>> GetEquipmentForExternalUser(GetEquipmentForExternalUserQuery query)
        {
            try
            {
                if (!string.IsNullOrEmpty(query?.EquipmentId))
                {
                    var dictionary = new Dictionary<string, object>();
                    var equipment = await _repository.GetItemAsync<PraxisEquipment>(eq => eq.ItemId == query.EquipmentId && !eq.IsMarkedToDelete);
                    dictionary.Add(EntityName.PraxisEquipment, equipment);
                    GetObjectArtifactsFromEquipment(dictionary, equipment);
                    return dictionary;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in GetEquipmentForExternalUser: {ex.Message}");
            }
            return null;
        }

        public async Task ProcessScheduledMaintenance(ProcessScheduledMaintenance command, CommandResponse response)
        {
            try
            {
                var maintenanceData = await _repository.GetItemAsync<PraxisEquipmentMaintenance>(m => !m.IsMarkedToDelete && m.ItemId == command.MaintenanceId);
                if (maintenanceData != null && command.MaintenanceAnswer != null)
                {
                    if (command.ForExternalUser)
                    {
                        var externalUserInfo = maintenanceData?.ExternalUserInfos?.Find(info => info?.SupplierInfo?.SupplierId == command.MaintenanceAnswer?.ReportedBy);
                        if (externalUserInfo != null)
                        {
                            externalUserInfo.Answer = command.MaintenanceAnswer;
                        }
                    }
                    else
                    {
                        var answers = maintenanceData?.Answers?.ToList() ?? new List<EquipmentMaintenanceAnswer>();
                        var answerIndex = answers.FindIndex(a => a.ItemId == command.MaintenanceAnswer?.ItemId);
                        if (answerIndex >= 0)
                        {
                            answers.RemoveAt(answerIndex);
                            answers.Insert(answerIndex, command.MaintenanceAnswer);
                        }
                        else
                        {
                            answers.Add(command.MaintenanceAnswer);
                        }

                        maintenanceData.CompletionStatus = command.CompletionStatus;
                        maintenanceData.CompletionStatusDetail = new RiqsActivityDetail()
                        {
                            PerformedBy = command.MaintenanceAnswer.ReportedBy,
                            PerformedOn = DateTime.UtcNow
                        };
                        maintenanceData.Answers = answers;
                    }

                    var submissionInfo = maintenanceData?.Answers?
                        .Select(answer => new PraxisUserSubmissionInfo
                        {
                            PraxisUserId = answer.ReportedBy,
                            SubmittedOn = answer.ReportedTime
                        })
                        .ToList();

                    await _repository.UpdateAsync(m => m.ItemId == maintenanceData.ItemId, maintenanceData);

                    await UpdatePraxisEquipmentMaintenanceDates(maintenanceData.ItemId, maintenanceData);
                    await _cockpitSummaryCommandService.CreateSummary(maintenanceData?.ItemId,
                        nameof(CockpitTypeNameEnum.PraxisEquipmentMaintenance), true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in ProcessScheduledMaintenance: {ex.Message}");
                response.SetError("command", ex.Message);
            }
        }

        public async Task UpdatePraxisEquipmentMaintenanceDates(string maintenanceItemId, PraxisEquipmentMaintenance maintenance)
        {
            try
            {
                _logger.LogInformation("Enter UpdatePraxisEquipmentMaintenanceDates ");
                var praxisEquipment = await _repository.GetItemAsync<PraxisEquipment>
                                (pe => pe.ItemId.Equals(maintenance.PraxisEquipmentId) && !pe.IsMarkedToDelete);

                if (praxisEquipment != null && praxisEquipment.MaintenanceDates.Any())
                {
                    MaintenanceDateProp toUpdateMaintenance =
                        praxisEquipment.MaintenanceDates.SingleOrDefault(md =>
                            md.ItemId.Equals(maintenanceItemId));

                    if (toUpdateMaintenance != null)
                    {
                        toUpdateMaintenance.Date = maintenance.MaintenanceEndDate;
                        toUpdateMaintenance.CompletionStatus = maintenance.CompletionStatus;
                        praxisEquipment.MaintenanceDates = praxisEquipment.MaintenanceDates.OrderBy(md => md.Date).ToList();

                        MaintenanceDatePropWithType toUpdateMaintenanceWithType = new MaintenanceDatePropWithType()
                        {
                            Date = toUpdateMaintenance.Date,
                            CompletionStatus = toUpdateMaintenance.CompletionStatus,
                            ScheduleType = maintenance.ScheduleType,
                            ItemId = toUpdateMaintenance.ItemId
                        };

                        UpdatePraxisEquipmentMaintenanceDatesMetaData(praxisEquipment, toUpdateMaintenanceWithType);
                        await _repository.UpdateAsync(pe => pe.ItemId.Equals(praxisEquipment.ItemId), praxisEquipment);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Got Error while updating praxisEquipment in EquipmentMaintenanceUpdatedEventHandler -> ${e}");
            }
        }

        public void UpdatePraxisEquipmentMaintenanceDatesMetaData(PraxisEquipment praxisEquipment, MaintenanceDatePropWithType toUpdateMaintenanceWithType)
        {
            _logger.LogInformation("Enter UpdatePraxisEquipmentMaintenanceDatesMetaData ");
            if (praxisEquipment.MetaDataList != null && praxisEquipment.MetaDataList.Any())
            {
                var maintenanceDatesEntry = praxisEquipment.MetaDataList
                    .FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.MaintenanceDates);
                if (maintenanceDatesEntry != null)
                {
                    try
                    {
                        var maintenanceDates = System.Text.Json.JsonSerializer.Deserialize<List<MaintenanceDatePropWithType>>(maintenanceDatesEntry.MetaData.Value);

                        var olUpdateMaintenanceWithType = maintenanceDates?.FirstOrDefault(x => x.ItemId == toUpdateMaintenanceWithType.ItemId);
                        if (olUpdateMaintenanceWithType == null)
                        {
                            maintenanceDates?.Add(toUpdateMaintenanceWithType);
                        }
                        else
                        {
                            olUpdateMaintenanceWithType.Date = toUpdateMaintenanceWithType.Date;
                            olUpdateMaintenanceWithType.CompletionStatus = toUpdateMaintenanceWithType.CompletionStatus;
                        }

                        maintenanceDatesEntry.MetaData.Value = System.Text.Json.JsonSerializer.Serialize(maintenanceDates);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Error occurred in UpdateMaintenanceDateWihType: {ex.Message}");
                    }

                }
                else
                {
                    praxisEquipment.MetaDataList.Add(GetEquipmentMaintenanceDatesMetaData(toUpdateMaintenanceWithType));
                }
            }
            else
            {
                var medataList = new List<MetaDataKeyPairValue>()
                {
                    GetEquipmentMaintenanceDatesMetaData(toUpdateMaintenanceWithType)
                };
                praxisEquipment.MetaDataList = medataList;
            }
        }


        private MetaDataKeyPairValue GetEquipmentMaintenanceDatesMetaData(MaintenanceDatePropWithType toUpdateMaintenanceWithType)
        {
            var maintenanceDates = new List<MaintenanceDatePropWithType>() { toUpdateMaintenanceWithType };
            return new MetaDataKeyPairValue
            {
                Key = EquipmentMetaDataKeys.MaintenanceDates,
                MetaData = new MetaValuePair
                {
                    Type = "Array",
                    Value = System.Text.Json.JsonSerializer.Serialize(maintenanceDates)
                }
            };

        }
        public async Task CreateMaintenance(CreateMaintenanceCommand command)
        {
            try
            {
                if (command?.MaintenanceDates?.Count > 0)
                {
                    var maintenanceIdsForAssignTask = new List<string>();
                    var roles = new List<string> { RoleNames.Admin, RoleNames.TaskController };
                    foreach (var date in command.MaintenanceDates)
                    {
                        var currentDate = DateTime.UtcNow;
                        var endDate = date.Date;
                        var startDate = endDate.AddDays(-1 * command.MaintenancePeriodDays);
                        var maintenance = command.Maintenance;
                        maintenance.ItemId = Guid.NewGuid().ToString();
                        maintenance.CreateDate = currentDate;
                        maintenance.RolesAllowedToRead = roles.ToArray();
                        maintenance.RolesAllowedToUpdate = roles.ToArray();
                        maintenance.RolesAllowedToDelete = roles.ToArray();
                        maintenance.IdsAllowedToRead = new string[] { };
                        maintenance.IdsAllowedToWrite = new string[] { };
                        maintenance.IdsAllowedToDelete = new string[] { };
                        maintenance.MaintenanceDate = startDate;
                        maintenance.MaintenanceEndDate = endDate;
                        maintenance.MaintenancePeriod = command.MaintenancePeriodDays;
                        await _repository.SaveAsync(maintenance);

                        AddRowLevelSecurity(maintenance.ItemId, maintenance.ClientId);
                        UpdatePraxisEquipmentMaintenanceDates(maintenance);

                        var mailSendDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                        if (mailSendDate >= maintenance.MaintenanceDate.Date)
                        {
                            _ = await ProcessEmailForResponsibleUsers(maintenance);
                            await _cockpitSummaryCommandService.CreateSummary(maintenance?.ItemId, nameof(CockpitTypeNameEnum.PraxisEquipmentMaintenance));
                            await _cockpitFormDocumentActivityMetricsGenerationService.OnCreateEquipmentMaintenanceFormGenerateActivityMetrics(maintenance);
                        }

                        if (currentDate.Date >= maintenance?.MaintenanceDate.Date && !string.IsNullOrEmpty(maintenance?.PraxisFormInfo?.FormId))
                        {
                            maintenanceIdsForAssignTask.Add(maintenance.ItemId);
                        }
                        _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(maintenance);
                    }

                    foreach (var maintenanceId in maintenanceIdsForAssignTask)
                    {
                        await AssignTasks(maintenanceId, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in CreateMaintenance: {ex.Message}");
            }
        }

        public void DeleteDependentEntitiesForEquipmentMaintenance(List<string> maintenanceIds)
        {
            try
            {
                if (maintenanceIds?.Count > 0)
                {
                    _cockpitSummaryCommandService.DeleteSummaryAsync(maintenanceIds, CockpitTypeNameEnum.PraxisEquipmentMaintenance).GetAwaiter();

                    _cockpitFormDocumentActivityMetricsGenerationService.OnDeleteTaskRemoveSummaryFromActivityMetrics(maintenanceIds,
                        nameof(PraxisEquipmentMaintenance)).GetAwaiter().GetResult();

                    var processGuides = _repository
                        .GetItems<PraxisProcessGuide>(p => maintenanceIds.Contains(p.RelatedEntityId))?.ToList() ?? new List<PraxisProcessGuide>();

                    var processGuideIds = processGuides.Select(pg => pg.ItemId).ToList();
                    _cockpitSummaryCommandService.DeleteSummaryAsync(processGuideIds, CockpitTypeNameEnum.PraxisProcessGuide).GetAwaiter();

                    _deleteTaskScheduleDataForPraxisProcessGuide.DeleteTask(processGuideIds,
                        TaskScheduleRemoveType.ForceAll).GetAwaiter().GetResult();

                    _praxisReportTemplateService.OnMaintenanceDeletedRemoveGeneratedReports(maintenanceIds).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in DeleteDependentEntities. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        public async Task AssignTasks(string maintenanceId, bool assignTask)
        {
            try
            {
                var maintenance = _repository.GetItem<PraxisEquipmentMaintenance>(m => m.ItemId == maintenanceId);
                var formId = maintenance?.PraxisFormInfo?.FormId ?? "";
                if (!string.IsNullOrEmpty(formId))
                {
                    string departmentId = maintenance?.ClientId ?? string.Empty;
                    var processGuide = !string.IsNullOrEmpty(maintenance?.ProcessGuideId) ? _repository
                          .GetItem<PraxisProcessGuide>(pg => pg.ItemId == maintenance.ProcessGuideId && !pg.IsMarkedToDelete) : null;

                    if (processGuide == null)
                    {
                        if (assignTask) await AssignTask(maintenance);
                    }
                    else
                    {
                        var equipment = _repository.GetItem<PraxisEquipment>(e => e.ItemId == maintenance.PraxisEquipmentId);
                        await UpdateProcessGuide(processGuide, maintenance, equipment);
                        await UpdateProcessGuideConfig(processGuide.PraxisProcessGuideConfigId, maintenance, equipment);
                        await UpdateProcessGuideTaskSchedule(processGuide.TaskSchedule?.ItemId, maintenance);
                        await UpdateProcessGuideTaskSummary(processGuide.TaskSchedule?.TaskSummaryId, maintenance);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        private async Task AssignTask(PraxisEquipmentMaintenance maintenance)
        {
            var formId = maintenance?.PraxisFormInfo?.FormId;
            var departmentId = maintenance.ClientId;
            var praxisForm = _repository.GetItems<PraxisForm>(s => s.ItemId == formId).FirstOrDefault();
            if (praxisForm == null)
            {
                _logger.LogError("PraxisForm not found with formId: {formId}", formId);
                return;
            }
            var processGuideConfig = await SaveNewProcessGuideConfig(praxisForm, departmentId, maintenance);
            var taskSchedulerModel = GetTaskSchedulerModel(processGuideConfig, praxisForm.Description, maintenance);

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_taskManagementServiceBaseUrl + "TaskManagementService/TaskManagementCommand/CreateTaskSchedule"),
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(taskSchedulerModel),
                        Encoding.UTF8,
                        "application/json")
                };
                var token = await _authUtilityService.GetAdminToken();
                request.Headers.Add("Authorization", $"bearer {token}");

                HttpResponseMessage response = await _serviceClient.SendToHttpAsync(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("Failed to create processguide with processguideConfigId -> {ItemId}", processGuideConfig.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError("Failed to create processguide with processguideConfigId -> {ItemId}", processGuideConfig.ItemId);
            }
        }

        private async Task<PraxisProcessGuideConfig> SaveNewProcessGuideConfig(PraxisForm praxisForm, string departmentId, PraxisEquipmentMaintenance maintenance)
        {
            var assignedUserIds = maintenance.ExecutivePersonIds?.ToList() ?? new List<string>();
            var Assignedclients = GetUserClients(assignedUserIds, departmentId, maintenance);

            var subissionDates = new List<DateTime>() { maintenance.MaintenanceEndDate.Date };
            var taskTimeTable = new PraxisTaskTimetable();
            taskTimeTable.SubmissionDates = subissionDates;

            var newPgConfig = new PraxisProcessGuideConfig()
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow,
                FormId = praxisForm.ItemId,
                TopicKey = praxisForm.TopicKey,
                TopicValue = praxisForm.TopicValue,
                Title = praxisForm.Title,
                TaskTimetable = taskTimeTable,
                Clients = Assignedclients,
                ControlledMembers = assignedUserIds,
                DueDate = maintenance.MaintenanceEndDate,
                RolesAllowedToRead = new string[] { RoleNames.Admin, RoleNames.AppUser },
                RolesAllowedToUpdate = new string[] { RoleNames.Admin, RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung },
                RolesAllowedToDelete = new string[] { RoleNames.Admin },
                IdsAllowedToDelete = new string[] { _securityContextProvider.GetSecurityContext().UserId }
            };

            await _repository.SaveAsync(newPgConfig);

            return newPgConfig;
        }

        private List<ProcessGuideClientInfo> GetUserClients(List<string> userIds, string departmentId, PraxisEquipmentMaintenance maintenance)
        {
            var clients = new List<ProcessGuideClientInfo>();
            var userClient = _repository.GetItem<PraxisClient>(c => c.ItemId == departmentId);
            var equipment = _repository.GetItem<PraxisEquipment>(c => c.ItemId == maintenance.PraxisEquipmentId);
            var client = new ProcessGuideClientInfo()
            {
                ClientId = userClient?.ItemId,
                ClientName = userClient?.ClientName,
                CategoryId = equipment?.CategoryId ?? "",
                CategoryName = equipment?.CategoryName ?? "",
                SubCategoryId = equipment?.SubCategoryId ?? "",
                SubCategoryName = equipment?.SubCategoryName ?? "",
                ControlledMembers = userIds,
                HasSpecificControlledMembers = userIds?.Count > 0
            };
            clients.Add(client);

            return clients;
        }

        private CreateTaskScheduleRequestModel GetTaskSchedulerModel(PraxisProcessGuideConfig processGuideConfig, string formName, PraxisEquipmentMaintenance maintenance)
        {
            var taskDatas = new List<TaskData>();
            var taskData = new TaskData()
            {
                HasRelatedEntity = true,
                HasTaskScheduleIntoRelatedEntity = true,
                RelatedEntityName = EntityName.PraxisProcessGuide,
                TaskSummaryId = Guid.NewGuid().ToString(),
                Title = processGuideConfig.Title,
                RelatedEntityObject = GetRelatedEntityObject(processGuideConfig, formName, maintenance)
            };
            taskDatas.Add(taskData);

            var submissionDates = new List<string> { maintenance.MaintenanceEndDate.ToString("yyyy-MM-dd") };
            var taskScheduleDetails = new TaskScheduleDetails()
            {
                HasToMoveNextDay = true,
                IsRepeat = false,
                SubmissionDates = submissionDates
            };

            return new CreateTaskScheduleRequestModel()
            {
                TaskScheduleDetails = taskScheduleDetails,
                TaskDatas = taskDatas,
                AssignMembers = new List<object>()
            };
        }

        private RelatedEntityObject GetRelatedEntityObject(PraxisProcessGuideConfig processGuideConfig, string formName, PraxisEquipmentMaintenance maintenance)
        {
            return new RelatedEntityObject()
            {
                ItemId = Guid.NewGuid().ToString(),
                FormId = processGuideConfig.FormId,
                FormName = formName,
                Title = processGuideConfig.Title,
                Tags = new[] { "Is-Valid-PraxisProcessGuide" },
                Language = "en-US",
                TopicKey = processGuideConfig.TopicKey,
                TopicValue = processGuideConfig.TopicValue,
                Description = processGuideConfig.Title,
                PatientDateOfBirth = maintenance.MaintenanceDate.Date,
                IsActive = true,
                ControlledMembers = processGuideConfig.ControlledMembers,
                Clients = processGuideConfig.Clients,
                ClientId = processGuideConfig.Clients.FirstOrDefault()?.ClientId,
                ClientName = processGuideConfig.Clients.FirstOrDefault()?.ClientName,
                DueDate = maintenance.MaintenanceEndDate.ToString("yyyy-MM-dd"),
                PraxisProcessGuideConfigId = processGuideConfig.ItemId,
                RelatedEntityId = maintenance.ItemId,
                RelatedEntityName = EntityName.PraxisEquipmentMaintenance
            };
        }

        private async Task UpdateProcessGuide(PraxisProcessGuide processGuide, PraxisEquipmentMaintenance maintenance, PraxisEquipment equipment)
        {
            var controllMembers = maintenance.ExecutivePersonIds?.ToList();
            controllMembers = controllMembers.Distinct().ToList();
            var clientList = (List<ProcessGuideClientInfo>)processGuide.Clients;
            if (clientList?.Count > 0)
            {
                clientList[0].ControlledMembers = controllMembers;
                clientList[0].CategoryId = equipment.CategoryId;
                clientList[0].CategoryName = equipment.CategoryName;
                clientList[0].SubCategoryId = equipment.SubCategoryId;
                clientList[0].SubCategoryName = equipment.SubCategoryName;
            }
            processGuide.Clients = clientList;
            processGuide.ControlledMembers = controllMembers;
            processGuide.DueDate = maintenance.MaintenanceEndDate;
            if (processGuide.TaskSchedule != null)
            {
                var fromDate = maintenance.MaintenanceEndDate.Date;
                var toDate = maintenance.MaintenanceEndDate.Date.AddDays(1).AddSeconds(-1);
                processGuide.TaskSchedule.TaskDateTime = fromDate;
                processGuide.TaskSchedule.FromDateTime = fromDate;
                processGuide.TaskSchedule.ToDateTime = toDate;
            }
            await _repository.UpdateAsync(pg => pg.ItemId == processGuide.ItemId, processGuide);
        }

        private async Task UpdateProcessGuideConfig(string configId, PraxisEquipmentMaintenance maintenance, PraxisEquipment equipment)
        {
            if (string.IsNullOrEmpty(configId)) return;
            var config = _repository.GetItem<PraxisProcessGuideConfig>(p => p.ItemId == configId);
            if (config != null)
            {
                var controllMembers = maintenance.ExecutivePersonIds?.ToList();
                controllMembers = controllMembers.Distinct().ToList();
                var clientList = config.Clients?.ToList();
                if (clientList?.Count > 0)
                {
                    clientList[0].ControlledMembers = controllMembers;
                    clientList[0].CategoryId = equipment.CategoryId;
                    clientList[0].CategoryName = equipment.CategoryName;
                    clientList[0].SubCategoryId = equipment.SubCategoryId;
                    clientList[0].SubCategoryName = equipment.SubCategoryName;
                }
                config.Clients = clientList;
                config.ControlledMembers = controllMembers;
                config.DueDate = maintenance.MaintenanceEndDate;
                if (config.TaskTimetable != null)
                {
                    config.TaskTimetable.SubmissionDates = new List<DateTime> { maintenance.MaintenanceEndDate.Date };
                }
                await _repository.UpdateAsync(pg => pg.ItemId == config.ItemId, config);
            }
        }

        private async Task UpdateProcessGuideTaskSchedule(string taskScheduleId, PraxisEquipmentMaintenance maintenance)
        {
            if (string.IsNullOrEmpty(taskScheduleId)) return;
            var taskSchedule = _repository.GetItem<TaskSchedule>(t => t.ItemId == taskScheduleId);
            if (taskSchedule != null)
            {
                var fromDate = maintenance.MaintenanceEndDate.Date;
                var toDate = maintenance.MaintenanceEndDate.Date.AddDays(1).AddSeconds(-1);
                taskSchedule.TaskDateTime = fromDate;
                taskSchedule.FromDateTime = fromDate;
                taskSchedule.ToDateTime = toDate;
                await _repository.UpdateAsync(t => t.ItemId == taskSchedule.ItemId, taskSchedule);
            }
        }

        private async Task UpdateProcessGuideTaskSummary(string taskSummaryId, PraxisEquipmentMaintenance maintenance)
        {
            if (string.IsNullOrEmpty(taskSummaryId)) return;
            var taskSummary = _repository.GetItem<TaskSummary>(t => t.ItemId == taskSummaryId);
            if (taskSummary != null)
            {
                taskSummary.SubmissionDates = new List<DateTime>() { maintenance.MaintenanceEndDate.Date };
            }
            await _repository.UpdateAsync(t => t.ItemId == taskSummary.ItemId, taskSummary);
        }

        public async Task UpdateMaintenanceForProcessGuideCreated(string maintenanceId, string processGuideId)
        {
            var maintenance = await _repository.GetItemAsync<PraxisEquipmentMaintenance>(m => m.ItemId == maintenanceId);
            if (maintenance != null)
            {
                maintenance.ProcessGuideId = processGuideId;
                await _repository.UpdateAsync(m => m.ItemId == maintenance.ItemId, maintenance);
            }
        }

        private void GetPraxisUsersFromMaintenance(Dictionary<string, object> dictionary, List<PraxisEquipmentMaintenance> maintenances)
        {
            var praxisUserIds = new List<string>();
            var ids = new List<string>();

            foreach (var maintenance in maintenances)
            {
                ids = maintenance?.ExecutivePersonIds?.ToList();
                if (ids?.Count > 0)
                {
                    praxisUserIds.AddRange(ids);
                }
                ids = maintenance?.ApprovedPersonIds?.ToList();
                if (ids?.Count > 0)
                {
                    praxisUserIds.AddRange(ids);
                }
                ids = maintenance?.Answers?.Select(a => a.ReportedBy)?.Where(b => !string.IsNullOrEmpty(b))?.ToList();
                if (ids?.Count > 0)
                {
                    praxisUserIds.AddRange(ids);
                }
                ids = maintenance?.LibraryFormResponses?.Select(a => a.CompletedBy)?.Where(b => !string.IsNullOrEmpty(b))?.ToList();
                if (ids?.Count > 0)
                {
                    praxisUserIds.AddRange(ids);
                }
                if (!string.IsNullOrEmpty(maintenance?.CompletionStatusDetail?.PerformedBy))
                {
                    praxisUserIds.Add(maintenance?.CompletionStatusDetail?.PerformedBy);
                }
            }
            praxisUserIds = praxisUserIds?.Distinct()?.ToList();

            if (praxisUserIds?.Count > 0)
            {
                var praxisUsers = _repository.GetItems<PraxisUser>(pu => praxisUserIds.Contains(pu.ItemId) || praxisUserIds.Contains(pu.UserId))?.Select(pu => new
                {
                    ItemId = pu.ItemId,
                    DisplayName = pu.DisplayName,
                    UserId = pu.UserId
                })?.ToList();

                dictionary.Add(EntityName.PraxisUser + 's', praxisUsers);
            }
        }
        private void GetObjectArtifactsFromMaintenance(Dictionary<string, object> dictionary, List<PraxisEquipmentMaintenance> maintenances)
        {
            var artifactIds = new List<string>();
            var ids = new List<string>();

            foreach (var maintenance in maintenances)
            {
                ids = maintenance?.LibraryForms?.Select(a => a.LibraryFormId)?.Where(b => !string.IsNullOrEmpty(b))?.ToList();
                if (ids?.Count > 0)
                {
                    artifactIds.AddRange(ids);
                }
            }
            artifactIds = artifactIds?.Distinct()?.ToList();
            if (artifactIds?.Count > 0)
            {
                var artifacts = _repository.GetItems<ObjectArtifact>(a => artifactIds.Contains(a.ItemId))?.Select(a => new
                {
                    ItemId = a.ItemId,
                    Name = a.Name,
                    FileStorageId = a.FileStorageId,
                    Extension = a.Extension
                })?.ToList();

                dictionary.Add(nameof(ObjectArtifact) + 's', artifacts);
            }
        }

        private void GetObjectArtifactsFromEquipment(Dictionary<string, object> dictionary, PraxisEquipment equipment)
        {
            var artifactIds = new List<string>();
            var ids = new List<string>();

            ids = equipment?.Files?.Select(f => f.DocumentId)?.Where(d => !string.IsNullOrEmpty(d))?.ToList();
            if (ids?.Count > 0)
            {
                artifactIds.AddRange(ids);
            }

            artifactIds = artifactIds?.Distinct()?.ToList();
            if (artifactIds?.Count > 0)
            {
                var artifacts = _repository.GetItems<ObjectArtifact>(a => artifactIds.Contains(a.ItemId))?.Select(a => new
                {
                    ItemId = a.ItemId,
                    Name = a.Name,
                    FileStorageId = a.FileStorageId,
                    Extension = a.Extension
                })?.ToList();

                dictionary.Add(nameof(ObjectArtifact) + 's', artifacts);
            }
        }

        public async Task<bool> ProcessEmailForResponsibleUsers(PraxisEquipmentMaintenance equipmentMaintenance)
        {
            try
            {
                var mailSendDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                if (!(mailSendDate >= equipmentMaintenance.MaintenanceDate.Date && mailSendDate <= equipmentMaintenance.MaintenanceEndDate)) return false;

                PraxisEquipment praxisEquipment = await
                _repository.GetItemAsync<PraxisEquipment>(pe => pe.ItemId.Equals(equipmentMaintenance.PraxisEquipmentId) && !pe.IsMarkedToDelete);

                if (!string.IsNullOrEmpty(equipmentMaintenance?.ItemId) && !string.IsNullOrEmpty(praxisEquipment?.ItemId))
                {
                    var personIds = new List<string>();
                    if (equipmentMaintenance.ExecutivePersonIds?.Count() > 0)
                    {
                        personIds.AddRange(equipmentMaintenance.ExecutivePersonIds.ToList());
                    }
                    if (equipmentMaintenance.ApprovedPersonIds?.Count() > 0)
                    {
                        personIds.AddRange(equipmentMaintenance.ApprovedPersonIds.ToList());
                    }
                    personIds = personIds.Distinct().ToList();

                    var emailTasks = new List<Task<bool>>();

                    foreach (var personId in personIds)
                    {
                        var person = _repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

                        if (!string.IsNullOrWhiteSpace(person?.Email))
                        {
                            var emailData = _emailDataBuilder.BuildEquipmentmaintenanceEmailData(praxisEquipment, equipmentMaintenance, person, praxisEquipment.ClientName);
                            var emailStatus = _emailNotifierService.SendMaintenanceScheduleEmail(person, emailData);
                            emailTasks.Add(emailStatus);
                        }
                    }

                    if (equipmentMaintenance.ExternalUserInfos?.Count > 0)
                    {
                        foreach (var externalInfo in equipmentMaintenance.ExternalUserInfos)
                        {
                            if (!string.IsNullOrEmpty(externalInfo?.SupplierInfo?.SupplierEmail))
                            {
                                var person = new Person()
                                {
                                    DisplayName = externalInfo.SupplierInfo.SupplierName,
                                    Email = externalInfo.SupplierInfo.SupplierEmail
                                };
                                var emailData = _emailDataBuilder.BuildEquipmentmaintenanceEmailData(praxisEquipment, equipmentMaintenance, person, praxisEquipment.ClientName, externalInfo);
                                var emailStatus = _emailNotifierService.SendMaintenanceScheduleEmail(person, emailData);
                                emailTasks.Add(emailStatus);
                            }
                        }
                    }

                    await Task.WhenAll(emailTasks);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"exception in ProcessEmailForResponsibleUsers -> {ex.Message}");
            }
            return false;
        }

        private void UpdatePraxisEquipmentMaintenanceDates(PraxisEquipmentMaintenance equipmentMaintenance)
        {
            try
            {
                PraxisEquipment praxisEquipment =
                    _repository.GetItem<PraxisEquipment>(pe => pe.ItemId.Equals(equipmentMaintenance.PraxisEquipmentId) && !pe.IsMarkedToDelete);

                if (praxisEquipment != null)
                {
                    var maintenanceDateData = new MaintenanceDateProp
                    {
                        ItemId = equipmentMaintenance.ItemId,
                        Date = equipmentMaintenance.MaintenanceEndDate,
                        CompletionStatus = equipmentMaintenance.CompletionStatus
                    };
                    if (praxisEquipment.MaintenanceDates == null)
                    {
                        List<MaintenanceDateProp> maintenanceDates = new List<MaintenanceDateProp>
                        {
                            maintenanceDateData
                        };

                        praxisEquipment.MaintenanceDates = maintenanceDates;
                    }
                    else
                    {
                        List<MaintenanceDateProp> maintenanceDates = praxisEquipment.MaintenanceDates.ToList();

                        maintenanceDates.Add(maintenanceDateData);
                        praxisEquipment.MaintenanceDates = maintenanceDates.OrderBy(md => md.Date).ToList();
                    }
                    MaintenanceDatePropWithType toUpdateMaintenanceWithType = new MaintenanceDatePropWithType()
                    {
                        Date = maintenanceDateData.Date,
                        CompletionStatus = maintenanceDateData.CompletionStatus,
                        ScheduleType = equipmentMaintenance.ScheduleType,
                        ItemId = maintenanceDateData.ItemId
                    };
                    UpdatePraxisEquipmentMaintenanceDatesMetaData(praxisEquipment, toUpdateMaintenanceWithType);
                    _repository.Update(pe => pe.ItemId.Equals(praxisEquipment.ItemId),
                        praxisEquipment);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Got Error while updating praxisEquipment in UpdatePraxisEquipmentMaintenanceDates -> ${e}");
            }
        }

        public async Task<PraxisGenericReportResult> PrepareEquipmentMaintenancePhotoDocumentationData(GetReportQuery filter)
        {
            var responses = new List<PraxisEquipmentMaintenanceForReport>();
            var metaDataList = new List<MetaData>();
            var clientIds = new List<string>();
            try
            {
                _logger.LogInformation("Preparing Equipment Maintenance PhotoDocumentation Data");
                var praxisEquipmentMaintenances =
                    (await _commonUtilService.GetEntityQueryResponse<PraxisEquipmentMaintenance>(
                        filter.FilterString,
                        filter.SortBy))?
                    .Results?
                    .ToList() ?? new List<PraxisEquipmentMaintenance>();
                foreach (var report in praxisEquipmentMaintenances)
                {
                    try
                    {
                        _logger.LogInformation("Equipment Name: {EquipmentTitle}", report.EquipmentTitle);
                        var generatedReport = new PraxisEquipmentMaintenanceForReport
                        {
                            EquipmentName = report.Title,
                            ProcessGuide = report.PraxisFormInfo?.FormName ?? "",
                            MaintenanceStartDate = report.MaintenanceDate.ToString(CultureInfo.InvariantCulture),
                            MaintenancePeriod = report.MaintenancePeriod,
                            Status = report.CompletionStatus?.Value,
                            Library = report.LibraryForms?.Select(form => form.LibraryFormName).ToList(),
                            Department = GetDepartmentByClientId(report.ClientId)?.Result?.ClientName,
                            Approver = GetPraxisUsersByIds(report.ApprovedPersonIds?.ToList() ?? new List<string>()),
                            ScheduleType = report.ScheduleType?.ToUpper() ?? "MAINTENANCE",
                            Remarks = report.Remarks,
                            Supplier = report.ExternalUserInfos?
                                    .Where(e => !string.IsNullOrEmpty(e.SupplierInfo?.SupplierName))?
                                    .Select(e => e.SupplierInfo.SupplierName)?.ToList() ?? new List<string>(),
                            MaintenanceEndDate = report.MaintenanceEndDate.Year >= 1000
                                ? report.MaintenanceEndDate.ToString(CultureInfo.InvariantCulture)
                                : report.MaintenanceDate.ToString(CultureInfo.InvariantCulture),
                            SupplierResponses = GetSupplierResponses(report.ExternalUserInfos)
                        };


                        var reportedByUserIds = report.Answers?
                            .Where(answer => answer.ReportedBy != null)
                            .Select(answer => answer.ReportedBy)
                            .ToList() ?? new List<string>();
                        generatedReport.CompletedBy = GetPraxisUsersByIds(reportedByUserIds);

                        generatedReport.ExecutingGroup = GetPraxisUsersByIds(report.ExecutivePersonIds?.ToList() ?? new List<string>());

                        if (report.ExecutivePersonIds == null || !report.ExecutivePersonIds.Any())
                        {
                            generatedReport.Pending = GetPendingExecutivePersons(report.ClientId);
                        }
                        else
                        {
                            var pendingExecutivePersonIds = report.ExecutivePersonIds
                                .Where(id => !reportedByUserIds.Contains(id))
                                .ToList() ?? new List<string>();
                            generatedReport.Pending = GetPraxisUsersByIds(pendingExecutivePersonIds);
                        }

                        generatedReport.Responses = report.Answers?
                                .Where(answer => answer != null)
                                .Select(GetEquipmentMaintenanceResponse)
                                .ToList() ??
                            new List<PraxisEquipmentMaintenanceResponse>();

                        responses.Add(generatedReport);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            $"Error occured while preparing EquipmentMaintenance PhotoDocumentation Data for '{report.Title}'"
                        );
                        _logger.LogError($"Error message: {e.Message} StackTrace: {e.StackTrace}");
                    }
                }

                clientIds = praxisEquipmentMaintenances?
                    .Where(equipment => equipment.ClientId != null)
                    .Select(id => id.ClientId)
                    .ToList() ?? new List<string>();
                metaDataList.Add(new MetaData()
                {
                    Name = "Maintenances",
                    Values = responses
                        .Select(answer =>
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                JsonConvert.SerializeObject(answer)))
                        .ToList()
                });

            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while trying to prepare EquipmentMaintenance PhotoDocumentation Data");
                _logger.LogError($"Error message: {e.Message} StackTrace: {e.StackTrace}");
            }
            return new PraxisGenericReportResult()
            {
                MetaDataList = metaDataList,
                ClientIds = clientIds.Distinct()
            };
        }

        private List<PraxisEquipmentMaintenanceResponse> GetSupplierResponses(
            List<PraxisEquipmentMaintenanceByExternalUser>? externalUsers)
        {
            if (externalUsers == null) return new List<PraxisEquipmentMaintenanceResponse>();
            var responses = new List<PraxisEquipmentMaintenanceResponse>();
            foreach (var user in externalUsers)
            {
                if (user.Answer != null)
                {
                    var answer = GetEquipmentMaintenanceResponse(user.Answer);
                    answer.ReportedByName = user.SupplierInfo.SupplierName;
                    responses.Add(answer);
                }
            }

            return responses;
        }
        private PraxisEquipmentMaintenanceResponse GetEquipmentMaintenanceResponse(EquipmentMaintenanceAnswer? answer)
        {
            if (answer == null) return new PraxisEquipmentMaintenanceResponse();
            return new PraxisEquipmentMaintenanceResponse
            {
                ItemId = answer.ItemId,
                Remarks = answer.Remarks,
                FileId = answer.FileId,
                ReportedBy = answer.ReportedBy,
                ReportedTime = answer.ReportedTime,
                ReportedByName = GetPraxisUserByIdAsync(answer.ReportedBy).Result?.DisplayName,
                Files = GetFileAsEquipmentMaintenanceFileByIds(answer.Files?.ToList() ?? new List<PraxisDocument>()),
                ApprovalResponse = answer.ApprovalResponse != null
                    ? new PraxisEquipmentMaintenanceResponseBase
                    {
                        ItemId = answer.ApprovalResponse.ItemId,
                        FileId = answer.ApprovalResponse.FileId,
                        Remarks = answer.ApprovalResponse.Remarks,
                        ReportedBy = answer.ApprovalResponse.ReportedBy,
                        ReportedTime = answer.ApprovalResponse.ReportedTime,
                        ReportedByName = GetPraxisUserByIdAsync(answer.ApprovalResponse.ReportedBy).Result?.DisplayName,
                        Files = GetFileAsEquipmentMaintenanceFileByIds(answer.ApprovalResponse.Files?.ToList() ?? new List<PraxisDocument>())
                    }
                    : null
            };
        }

        private async Task<PraxisClient> GetDepartmentByClientId(string clientId)
        {
            return await _repository
                .GetItemAsync<PraxisClient>(pc => pc.ItemId.Equals(clientId));
        }

        private async Task<PraxisUser?> GetPraxisUserByIdAsync(string? userId)
        {
            if (userId == null) return null;
            return await _repository
                .GetItemAsync<PraxisUser>(user => user.ItemId.Equals(userId));
        }
        private List<string> GetPraxisUsersByIds(List<string> ids)
        {
            return _repository
                       .GetItems<PraxisUser>(item => ids.Contains(item.ItemId))?
                       .Where(user => user.DisplayName != null)
                       .Select(user => user.DisplayName)
                       .ToList() ?? new List<string>();
        }
        private List<string> GetPendingExecutivePersons(string clientId)
        {
            var users = _repository
                .GetItems<PraxisUser>(user => user.ClientId.Equals(clientId) && !user.Roles.Contains(RoleNames.GroupAdmin))?
                .ToList();
            return users?
                .Where(user => user.DisplayName != null)
                .Select(user => user.DisplayName)
                .ToList() ?? new List<string>();
        }

        private IEnumerable<PraxisDocument> GetFileAsEquipmentMaintenanceFileByIds(List<PraxisDocument> praxisFiles)
        {
            var files = new List<PraxisDocument>();
            foreach (var file in praxisFiles)
            {
                var response = _praxisFileService.GetFileInformation(file.DocumentId);
                if (response != null)
                {
                    files.Add(new PraxisDocument
                    {
                        DocumentId = response.ItemId,
                        DocumentName = response.Name,
                        CreatedOn = response.CreateDate,
                        IsDeleted = false,
                        FileType = GetFileType(response.Name)
                    });
                }
            }

            return files;
        }
        private string GetFileType(string fileName)
        {
            var extension = fileName?.Split('.')?.Last()?.ToLowerInvariant();
            return ReportConstants.ImageExts.Contains(extension) ? "image" : "other";
        }

        public void SendMailOnEquipmentMaintenanceDelete(PraxisEquipmentMaintenance equipmentMaintenance, string equipmentName)
        {
            var emailTasks = new List<Task<bool>>();
            if (equipmentMaintenance.ExternalUserInfos?.Count > 0)
            {
                foreach (var externalUser in equipmentMaintenance.ExternalUserInfos)
                {
                    if (string.IsNullOrWhiteSpace(externalUser?.SupplierInfo?.SupplierEmail)) continue;
                    var person = new Person
                    {
                        DisplayName = externalUser.SupplierInfo.SupplierName,
                        Email = externalUser.SupplierInfo.SupplierEmail
                    };
                    var emailData = _emailDataBuilder.BuildMaintenanceDeleteEmailData(person,
                        equipmentMaintenance.ScheduleType ?? "MAINTENANCE",
                        equipmentName);
                    var emailStatus =
                        _emailNotifierService.SendMaintenanceDeleteEmail(person, emailData);
                    emailTasks.Add(emailStatus);
                }
            }

            Task.WhenAll(emailTasks).GetAwaiter().GetResult();
        }

        public async Task<PraxisEquipmentMaintenanceSupplierInfo> GetEquipmentMaintenanceSupplierInfo(string equipementId, string supplierId)
        {
            try
            {
                if (!string.IsNullOrEmpty(equipementId) && !string.IsNullOrEmpty(supplierId))
                {

                    var equipmentMaintenance = await _repository.GetItemAsync<PraxisEquipmentMaintenance>(eq => eq.PraxisEquipmentId == equipementId && !eq.IsMarkedToDelete && eq.ExternalUserInfos != null && eq.ExternalUserInfos.Any(y => y.SupplierInfo.SupplierId.Equals(supplierId)));
                    var suppilerInfo = equipmentMaintenance?.ExternalUserInfos?.Find(x => x.SupplierInfo.SupplierId == supplierId)?.SupplierInfo;
                    return suppilerInfo ?? new PraxisEquipmentMaintenanceSupplierInfo();
                }
                return new PraxisEquipmentMaintenanceSupplierInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in GetEquipmentForExternalUser: {ex.Message}");
            }
            return new PraxisEquipmentMaintenanceSupplierInfo();
        }
    }

}