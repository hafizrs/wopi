using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SKO;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

#nullable enable
public class CirsReportUpdateService : ICirsReportUpdateService
{
    private readonly ILogger<CirsReportUpdateService> _logger;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IRepository _repository;
    private readonly IChangeLogService _changeLogService;
    private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
    private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
    private readonly IGenericEventPublishService _genericEventPublishService;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly ICirsReportCreateService _cirsReportCreateService;
    private readonly IActiveInactiveCirsReportService _activeInactiveCirsReportService;
    private readonly IServiceClient _serviceClient;
    public CirsReportUpdateService(
        ILogger<CirsReportUpdateService> logger,
        ISecurityContextProvider securityContextProvider,
        IRepository repository,
        IChangeLogService changeLogService,
        IObjectArtifactUtilityService objectArtifactUtilityService,
        IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
        ICockpitSummaryCommandService cockpitSummaryCommandService,
        IGenericEventPublishService genericEventPublishService,
        ICirsPermissionService cirsPermissionService,
        ICirsReportCreateService cirsReportCreateService,
        IActiveInactiveCirsReportService activeInactiveCirsReportService,
        IServiceClient serviceClient)
    {
        _logger = logger;
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _changeLogService = changeLogService;
        _objectArtifactUtilityService = objectArtifactUtilityService;
        _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
        _genericEventPublishService = genericEventPublishService;
        _cirsPermissionService = cirsPermissionService;
        _cirsReportCreateService = cirsReportCreateService;
        _activeInactiveCirsReportService = activeInactiveCirsReportService;
        _serviceClient = serviceClient;
    }

    public async Task InitiateUpdateAsync(AbstractUpdateCirsReportCommand command)
    {
        var cirsReport = await GetCirsReportByIdAsync(command.CirsReportId);
        var isValidStatusUpdateRequest = IsValidStatusUpdateRequest(
            command.CirsDashboardName,
            currentStatus: cirsReport.Status,
            newStatus: command.Status);
        if (isValidStatusUpdateRequest == false)
        {
            throw new InvalidOperationException("INVALID_STATUS_MOVE");
        }

        if (cirsReport != null &&
            cirsReport.Tags.FirstOrDefault() == command.Tags.First() &&
            isValidStatusUpdateRequest != false &&
            IsValidRankDetails(cirsReport, command.Status, command.RankDetails))
        {
            var isColumnChanged = command.Status != cirsReport.Status;
            var genericUpdates = PrepareCirsReportUpdateData(cirsReport, command);
            var specificUpdates = PrepareDashboardSpecificUpdateData(cirsReport, command);

            var praxisClientId = cirsReport.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId ?? string.Empty;
            var permission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(
                            praxisClientId,
                            cirsReport.CirsDashboardName, true);
            var rolesDisallowedToRead = _cirsPermissionService.PrepareRolesDisallowedToRead(cirsReport.CirsDashboardName, command.ReportingVisibility, permission);

            specificUpdates[nameof(CirsGenericReport.RolesDisallowedToRead)] = rolesDisallowedToRead;
            cirsReport.RolesDisallowedToRead = rolesDisallowedToRead;

            var updates = (genericUpdates ?? new Dictionary<string, object>())
                .Concat(specificUpdates ?? new Dictionary<string, object>())
                .DistinctBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (command.IsDuplicate == true)
            {
                await _cirsReportCreateService.DuplicateCirsReport(cirsReport, updates);

                var activeInactiveCommand = new ActiveInactiveCirsReportCommand()
                {
                    CirsReportId = cirsReport.ItemId,
                    MarkAsActive = false
                };
                await _activeInactiveCirsReportService.InitiateActiveInactiveAsync(activeInactiveCommand);
            }
            else if (updates.Count > 0)
            {
                await UpdateCirsReportsAsync(command, cirsReport, updates!);
                PublishCirsReportEvent(cirsReport.ItemId, isColumnChanged, true);
                _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(cirsReport);
                
            }
        }
    }

    private void PublishCirsReportEvent(string reportId, bool isColumnChanged, bool isUpdate)
    {
        var cirsReportEvent = new GenericEvent
        {
            EventType = PraxisEventType.CirsReportEvent,
            JsonPayload = JsonConvert.SerializeObject(new CirsReportEvent() { ReportId = reportId, IsColumnChanged = isColumnChanged, IsUpdate = isUpdate })
        };

        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), cirsReportEvent);
    }

    public async Task UpdateLibraryFormResponse(ObjectArtifact artifact)
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

                    if (entityName == EntityName.CirsGenericReport && !string.IsNullOrEmpty(entityId))
                    {
                        var cirsReport = _repository.GetItem<CirsGenericReport>
                                        (p => p.ItemId == entityId && !p.IsMarkedToDelete);

                        if (cirsReport != null)
                        {
                            var libraryFormResponse = cirsReport?.LibraryFormResponses?
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
                                var responses = cirsReport?.LibraryFormResponses?.ToList() ?? new List<PraxisLibraryFormResponse>();
                                responses.Add(libraryFormResponse);
                                cirsReport.LibraryFormResponses = responses;
                            }
                            await _repository.UpdateAsync(p => p.ItemId == cirsReport.ItemId, cirsReport);
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

    private Dictionary<string, object> PrepareDashboardSpecificUpdateData(
        CirsGenericReport cirsReport,
        AbstractUpdateCirsReportCommand updateCommand)
    {
        return updateCommand switch
        {
            UpdateComplainReportCommand command => PrepareDashboardSpecificUpdateData(cirsReport, command),
            UpdateHintReportCommand command => PrepareDashboardSpecificUpdateData(cirsReport, command),
            UpdateAnotherMessageCommand command => PrepareDashboardSpecificUpdateData(cirsReport, command),
            UpdateIdeaReportCommand command => PrepareDashboardSpecificUpdateData(cirsReport, command),
            UpdateIncidentReportCommand command => PrepareDashboardSpecificUpdateData(cirsReport, command),
            UpdateFaultReportCommand command => PrepareDashboardSpecificUpdateData(cirsReport, command),
            _ => new Dictionary<string, object>(),
        };
    }

    private Dictionary<string, object> PrepareDashboardSpecificUpdateData(
        CirsGenericReport cirsReport,
        UpdateComplainReportCommand command)
    {
        var updates = new Dictionary<string, object>();
        var existingMetadata = cirsReport.MetaData ?? new Dictionary<string, object?>();
        var isMetadataUpdated = false;

        var metadataUpdates = new Dictionary<string, object?>
        {
            {$"{CommonCirsMetaKey.ResponseText}", command.ResponseText},
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString() }
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metadataUpdates[key] = value;
            }
        }

        foreach (var update in metadataUpdates)
        {
            if (update.Value != null)
            {
                isMetadataUpdated = true;
                existingMetadata[update.Key] = update.Value;
            }
        }

        if (isMetadataUpdated)
        {
            updates[nameof(CirsGenericReport.MetaData)] = existingMetadata;
            cirsReport.MetaData = existingMetadata;
        }
        if (command.OriginatorInfo != null)
        {
            updates[nameof(CirsGenericReport.OriginatorInfo)] = command.OriginatorInfo;
            cirsReport.OriginatorInfo = command.OriginatorInfo;
        }

        return updates;
    }

    private static Dictionary<string, object> PrepareDashboardSpecificUpdateData(
        CirsGenericReport cirsReport,
        UpdateHintReportCommand command)
    {
        var updates = new Dictionary<string, object>();
        var existingMetadata = cirsReport.MetaData;

        var metadataUpdates = new Dictionary<string, object?>
        {
            {$"{HintMetaKey.ReportingDate}", command.ReportingDate},
            {$"{CommonCirsMetaKey.ReporterClientId}", command.ReporterClientId},
            {$"{CommonCirsMetaKey.ReportInternalOffice}", command.ReportInternalOffice},
            {$"{CommonCirsMetaKey.ReportExternalOffice}", command.ReportExternalOffice},
            {$"{CommonCirsMetaKey.DecisionSelection}", command.DecisionSelection},
            {$"{CommonCirsMetaKey.DecisionSelectionReason}", command.DecisionSelectionReason}
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metadataUpdates[key] = value;
            }
        }

        foreach (var update in metadataUpdates)
        {
            if (update.Value != null)
                existingMetadata[update.Key] = update.Value;
        }

        updates.Add(nameof(CirsGenericReport.MetaData), existingMetadata);
        cirsReport.MetaData = existingMetadata;

        if (command.ExternalReporters?.Count > 0)
        {
            cirsReport.ExternalReporters = new List<ExternalReporter>();
            foreach (var externalReporter in command.ExternalReporters)
            {
                cirsReport.ExternalReporters.Add
                (
                    new ExternalReporter()
                    {
                        SupplierInfo = externalReporter,
                        Remarks = ""
                    }
                );
            }
            updates.Add(nameof(CirsGenericReport.ExternalReporters), cirsReport.ExternalReporters);
        }

        return updates;
    }

    private static Dictionary<string, object> PrepareDashboardSpecificUpdateData(
        CirsGenericReport cirsReport,
        UpdateAnotherMessageCommand command)
    {
        var updates = new Dictionary<string, object>();
        var existingMetadata = cirsReport.MetaData ?? new Dictionary<string, object?>();
        var isMetadataUpdated = false;

        var metadataUpdates = new Dictionary<string, object?>
        {
            {$"{CommonCirsMetaKey.ResponseText}", command.ResponseText},
            {$"{CommonCirsMetaKey.ReporterClientId}", command.ReporterClientId},
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString() },
            {$"{AnotherMetaKey.ImplementationProposal}", command.ImplementationProposal?.ToString() }
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metadataUpdates[key] = value;
            }
        }

        foreach (var update in metadataUpdates)
        {
            if (update.Value != null)
            {
                isMetadataUpdated = true;
                existingMetadata[update.Key] = update.Value;
            }
        }

        if (isMetadataUpdated)
        {
            updates[nameof(CirsGenericReport.MetaData)] = existingMetadata;
            cirsReport.MetaData = existingMetadata;
        }
        if (command.OriginatorInfo != null)
        {
            updates[nameof(CirsGenericReport.OriginatorInfo)] = command.OriginatorInfo;
            cirsReport.OriginatorInfo = command.OriginatorInfo;
        }

        return updates;
    }

    private static Dictionary<string, object> PrepareDashboardSpecificUpdateData(
        CirsGenericReport cirsReport,
        UpdateIdeaReportCommand command)
    {
        var updates = new Dictionary<string, object>();
        var existingMetadata = cirsReport.MetaData;

        var metadataUpdates = new Dictionary<string, object?>
        {
            {$"{IdeaMetaKey.TargetGroup}", command.TargetGroup},
            {$"{IdeaMetaKey.Requirements}", command.Requirements},
            {$"{CommonCirsMetaKey.ReporterClientId}", command.ReporterClientId},
            {$"{IdeaMetaKey.BenefitOfIdea}", command.BenefitOfIdea},
            {$"{IdeaMetaKey.FeasibilityAndResourceRequirements}", command.FeasibilityAndResourceRequirements},
            {$"{CommonCirsMetaKey.DecisionSelection}", command.DecisionSelection},
            {$"{CommonCirsMetaKey.DecisionSelectionReason}", command.DecisionSelectionReason}
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metadataUpdates[key] = value;
            }
        }

        foreach (var update in metadataUpdates)
        {
            if (update.Value != null)
                existingMetadata[update.Key] = update.Value;
        }

        updates.Add(nameof(CirsGenericReport.MetaData), existingMetadata);
        cirsReport.MetaData = existingMetadata;

        return updates;
    }

    private Dictionary<string, object> PrepareDashboardSpecificUpdateData(
        CirsGenericReport cirsReport,
        UpdateIncidentReportCommand command)
    {
        var updates = new Dictionary<string, object>();

        var existingMetadata = cirsReport.MetaData;

        var metadataUpdates = new Dictionary<string, object?>
        {
            {$"{IncidentMetaKey.Topic}", command.Topic},
            {$"{IncidentMetaKey.Measures}", command.Measures},
            {$"{CommonCirsMetaKey.ReportExternalOffice}", command.ReportExternalOffice},
            {$"{CommonCirsMetaKey.ReportInternalOffice}", command.ReportInternalOffice},
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString()}
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metadataUpdates[key] = value;
            }
        }

        foreach (var update in metadataUpdates)
        {
            if (update.Value != null)
                existingMetadata[update.Key] = update.Value;
        }

        updates.Add(nameof(CirsGenericReport.MetaData), existingMetadata);
        cirsReport.MetaData = existingMetadata;

        if (command.ExternalReporters?.Count > 0)
        {
            cirsReport.ExternalReporters = new List<ExternalReporter>();
            foreach (var externalReporter in command.ExternalReporters)
            {
                cirsReport.ExternalReporters.Add
                (
                    new ExternalReporter()
                    {
                        SupplierInfo = externalReporter,
                        Remarks = ""
                    }
                );
            }
            updates.Add(nameof(CirsGenericReport.ExternalReporters), cirsReport.ExternalReporters);
        }

        return updates;
    }

    private Dictionary<string, object> PrepareDashboardSpecificUpdateData(
        CirsGenericReport cirsReport,
        UpdateFaultReportCommand command)
    {
        var updates = new Dictionary<string, object>();
        var existingMetadata = cirsReport.MetaData ?? new Dictionary<string, object?>();
        var isMetadataUpdated = false;

        var metadataUpdates = new Dictionary<string, object?>()
        {
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString()}
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metadataUpdates[key] = value;
            }
        }

        foreach (var update in metadataUpdates)
        {
            if (update.Value != null)
            {
                isMetadataUpdated = true;
                existingMetadata[update.Key] = update.Value;
            }
        }

        if (isMetadataUpdated)
        {
            updates[nameof(CirsGenericReport.MetaData)] = existingMetadata;
            cirsReport.MetaData = existingMetadata;
        }

        return updates;
    }

    private static Dictionary<string, object> PrepareCirsReportUpdateData(
        CirsGenericReport cirsReport,
        AbstractUpdateCirsReportCommand command)
    {
        List<string> updateAbleProps = new() {
            nameof(CirsGenericReport.Title),
            nameof(CirsGenericReport.Status),
            nameof(CirsGenericReport.Remarks),
            nameof(CirsGenericReport.AttachmentIds),
            nameof(CirsGenericReport.KeyWords),
            nameof(CirsGenericReport.ClientId),
            nameof(CirsGenericReport.Description)
        };

        var updates = new Dictionary<string, object>() { };
        command.AttachmentIds = command.AttachmentIds?.Distinct();
        foreach (PropertyInfo prop in command.GetType().GetProperties().Where(property => updateAbleProps.Contains(property.Name)))
        {
            var value = prop.GetValue(command, null);
            if (value != null)
            {
                updates.Add(prop.Name, value);
            }
        }

        if (command.ResponsibleUsers?.Count() > 0)
        {
            updates[nameof(cirsReport.ResponsibleUsers)] = command.ResponsibleUsers;
            cirsReport.ResponsibleUsers = command.ResponsibleUsers;
        }

        if (command.AffectedInvolvedParties?.Count() > 0)
        {
            updates[nameof(cirsReport.AffectedInvolvedParties)] = command.AffectedInvolvedParties;
            cirsReport.AffectedInvolvedParties = command.AffectedInvolvedParties;
        }

        if (command.AttachedDocuments != null)
        {
            updates[nameof(cirsReport.AttachedDocuments)] = command.AttachedDocuments;
            cirsReport.AttachedDocuments = command.AttachedDocuments;
        }
        if (command.AttachedForm != null || 
            (!string.IsNullOrEmpty(cirsReport?.AttachedForm?.FileStorageId) && 
             !(command?.AttachmentIds?.Contains(cirsReport?.AttachedForm?.FileStorageId) ?? false)))
        {
            updates[nameof(cirsReport.AttachedForm)] = command.AttachedForm;
            cirsReport.AttachedForm = command.AttachedForm;
        }

        if (command.CirsEditHistory != null)
        {
            var editHistories = cirsReport?.CirsEditHistory ?? new List<CirsEditHistory>();
            foreach (var history in command.CirsEditHistory)
            {
                if (!(history.CirsActivityPerformerModel?.Count > 0)) continue;

                var edited = editHistories.FirstOrDefault(e => e.PropertyName == history.PropertyName);
                //var performer = edited?.CirsActivityPerformerModel?.FirstOrDefault(a => a.PerformedBy == history.CirsActivityPerformerModel[0].PerformedBy);
                //if (performer != null)
                //{
                //    performer.PerformedOn = history.CirsActivityPerformerModel[0].PerformedOn;
                //}
                if (edited != null)
                {
                    edited.CirsActivityPerformerModel ??= new List<CirsActivityPerformerModel>();
                    edited.CirsActivityPerformerModel.Add(history.CirsActivityPerformerModel[0]);
                }
                else
                {
                    editHistories.Add(history);
                }
            }
            updates[nameof(cirsReport.CirsEditHistory)] = editHistories;
            cirsReport.CirsEditHistory = editHistories;
        }

        return updates;
    }

    private async Task<CirsGenericReport> GetCirsReportByIdAsync(string cirsReportId)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        return await _repository.GetItemAsync<CirsGenericReport>(i =>
            i.ItemId == cirsReportId &&
            !i.IsMarkedToDelete);
    }

    private static bool? IsValidStatusUpdateRequest(
        CirsDashboardName dashboardName,
        string currentStatus,
        string? newStatus)
    {
        if (string.IsNullOrWhiteSpace(newStatus)) return null;

        var statusEnumValues = dashboardName.GetCirsReportStatusEnumValues();

        var cirsReportStatusUpdateDirectionMap = statusEnumValues
            .Select((status, index) =>
                new KeyValuePair<string, List<string>>(
                    status,
                    GetFollowingStatuses(statusEnumValues, index)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var isValid = false;

        if (cirsReportStatusUpdateDirectionMap.TryGetValue(currentStatus, out var statusChangeDirection))
        {
            isValid = statusChangeDirection.Contains(newStatus);
        }

        return isValid;
    }

    private static List<string> GetFollowingStatuses(List<string> statusEnumValues, int index)
    {
        if (statusEnumValues.Count == 4 && index == 0)
            return statusEnumValues.Take(2).ToList();

        return index == statusEnumValues.Count - 1
            ? statusEnumValues.Skip(index - 1).ToList()
            : statusEnumValues.Skip(index).ToList();
    }

    private async Task<bool> UpdateCirsReportsAsync(
        AbstractUpdateCirsReportCommand command,
        CirsGenericReport cirsReport,
        Dictionary<string, object?> cirsReportUpdates)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var currentDateTime = DateTime.UtcNow.ToLocalTime();
        var clientId = cirsReport.AffectedInvolvedParties?.Select(a => a.PraxisClientId)?.FirstOrDefault() ?? string.Empty;

        var cirsReportUpdateTasks = new List<Task>();

        if (IsRankChanged(cirsReport, command.Status, command.RankDetails))
        {
            var cirsIds = new List<string>();

            if (command.RankDetails != null && !string.IsNullOrWhiteSpace(command.RankDetails?.RankAfterId))
            {
                cirsIds.Add(command.RankDetails.RankAfterId);
            }
            if (command.RankDetails != null && !string.IsNullOrWhiteSpace(command.RankDetails?.RankBeforeId))
            {
                cirsIds.Add(command.RankDetails.RankBeforeId);
            }
            ulong rank = 0;
            if (cirsIds.Count > 0) 
            {
                var reports = GetCirsReportByIds(cirsIds.ToArray());
                var prev = !string.IsNullOrEmpty(command.RankDetails?.RankAfterId) ? 
                            reports?.FirstOrDefault(r => r.ItemId == command.RankDetails?.RankAfterId) : null;
                var next = !string.IsNullOrEmpty(command.RankDetails?.RankBeforeId) ?
                            reports?.FirstOrDefault(r => r.ItemId == command.RankDetails?.RankBeforeId) : null;

                if (next != null) rank = next.Rank;
                else if (prev != null) rank = prev.Rank + 1;

                cirsReport.Rank = rank;
                cirsReportUpdateTasks.Add(UpdateNextAllReportsRank(cirsReport, clientId, command.Status ?? cirsReport.Status));
            }
            else
            {
                rank = GetNextRankValue(clientId, cirsReport.CirsDashboardName, command.Status ?? cirsReport.Status);
                cirsReport.Rank = rank;
            }

            cirsReportUpdates.Add(nameof(CirsGenericReport.Rank), rank);
        }

        _cirsPermissionService.SetCirsReportPermission(cirsReport);
        cirsReportUpdates.Add(nameof(CirsGenericReport.RolesAllowedToRead), cirsReport.RolesAllowedToRead);
        cirsReportUpdates.Add(nameof(CirsGenericReport.IdsAllowedToRead), cirsReport.IdsAllowedToRead);

        await Task.WhenAll(cirsReportUpdateTasks);

        await UpdateCirsReportAsync(command, cirsReport, cirsReportUpdates, currentDateTime);
        await _cockpitSummaryCommandService.CreateSummary(cirsReport.ItemId,
            nameof(CockpitTypeNameEnum.CirsGenericReport), true);
        return true;
    }

    private ulong GetNextRankValue(string clientId, CirsDashboardName cirsDashboardName, string status)
    {
        ulong lastRank = _repository.GetItems<CirsGenericReport>
                (c => c.AffectedInvolvedParties != null && c.AffectedInvolvedParties.Any(a => a.PraxisClientId == clientId) &&
                c.CirsDashboardName == cirsDashboardName && c.Status == status)?.OrderByDescending(c => c.Rank)?.FirstOrDefault()?.Rank ?? 0;
        return lastRank + 1;
    }

    private async Task UpdateNextAllReportsRank(CirsGenericReport report, string clientId, string status)
    {
        try
        {
            var filterBuilder = Builders<CirsGenericReport>.Filter;

            var filter =
                filterBuilder.Ne(r => r.ItemId, report.ItemId) &
                filterBuilder.Eq(r => r.CirsDashboardName, report.CirsDashboardName) &
                filterBuilder.Eq(r => r.Status, status) &
                filterBuilder.Gte(r => r.Rank, report.Rank) &
                filterBuilder.Where(r =>
                    r.AffectedInvolvedParties != null &&
                    r.AffectedInvolvedParties.Any(party => party.PraxisClientId == clientId));

            ulong incValue = 1;

            var update = Builders<CirsGenericReport>.Update
                .Inc(nameof(CirsGenericReport.Rank), incValue);


            var db = _ecapMongoDbDataContextProvider.GetTenantDataContext();
            await db.GetCollection<CirsGenericReport>($"{EntityName.CirsGenericReport}s").UpdateManyAsync(filter, update);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occrued in UpdateNextAllReportsRank -> {ex.Message}");
        }
    }

    private async Task<bool> UpdateCirsReportAsync(
        AbstractUpdateCirsReportCommand command,
        CirsGenericReport cirsReport,
        Dictionary<string, object?> cirsReportUpdates,
        DateTime currentDateTime)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        var filter = Builders<BsonDocument>.Filter.Eq("_id", cirsReport.ItemId);

        cirsReportUpdates.Add(nameof(CirsGenericReport.LastUpdateDate), currentDateTime);
        cirsReportUpdates.Add(nameof(CirsGenericReport.LastUpdatedBy), securityContext.UserId);
        GenerateCirsReportStatusChangeLog(cirsReportUpdates, cirsReport, command.Status, currentDateTime, securityContext.UserId);

        return await _changeLogService.UpdateChange(nameof(CirsGenericReport), filter, cirsReportUpdates);
    }

    private void GenerateCirsReportStatusChangeLog(
        Dictionary<string, object?> cirsReportUpdates,
        CirsGenericReport cirsReport,
        string? newStatus,
        DateTime changedOn,
        string changedBy)
    {
        if (!string.IsNullOrWhiteSpace(newStatus)) return;
        if (cirsReport.Status != newStatus)
        {
            var statusChangeLog = cirsReport.StatusChangeLog ?? new List<StatusChangeEvent>();
            var statusChangeEvent = new StatusChangeEvent()
            {
                PreviousStatus = cirsReport.Status,
                CurrentStatus = newStatus,
                ChangedOn = changedOn,
                ChangedBy = changedBy
            };
            statusChangeLog.Add(statusChangeEvent);
            cirsReportUpdates.Add(nameof(CirsGenericReport.StatusChangeLog), statusChangeLog);
        }
    }

    private bool IsRankChanged(CirsGenericReport cirsReport, string? newStatus, RankDetails? rankDetails)
    {
        var isChanged = true;
        if (string.IsNullOrEmpty(newStatus))
        {
            return isChanged;
        }
        else if (rankDetails != null)
        {
            if (cirsReport.Status == newStatus && string.IsNullOrEmpty(rankDetails.RankAfterId) && string.IsNullOrEmpty(rankDetails.RankBeforeId))
            {
                isChanged = false;
            }
        }
        else if (cirsReport.Status == newStatus)
        {
            isChanged = false;
        }

        return isChanged;
    }

    private bool IsValidRankDetails(
        CirsGenericReport cirsReport,
        string? newStatus,
        RankDetails? rankDetails)
    {
        var isValid = false;
        if (rankDetails != null)
        {
            var ids = new string?[]
            {
                rankDetails.RankBeforeId,
                rankDetails.RankAfterId
            }
            .Where(id => !string.IsNullOrWhiteSpace(id) && id != cirsReport.ItemId)
            .Distinct().ToArray();

            if (ids.Length > 0)
            {
                var cirsReports = GetCirsReportByIds(ids!);
                if (cirsReports.Count == ids.Length)
                {
                    if (!string.IsNullOrWhiteSpace(rankDetails.RankAfterId) || !string.IsNullOrWhiteSpace(rankDetails.RankBeforeId))
                    {
                        isValid = true;
                    }
                    else if (cirsReport.Status != newStatus)
                    {
                        isValid = true;
                    }
                }
            }
        }
        else
        {
            isValid = true;
        }

        return isValid;
    }

    private List<CirsGenericReport> GetCirsReportByIds(string[] cirsReportIds)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        return _repository.GetItems<CirsGenericReport>(i =>
                cirsReportIds.Contains(i.ItemId) &&
                !i.IsMarkedToDelete &&
                i.IsActive)
            .ToList();
    }

    public async Task UpdateFaultPermissions(string equipmentId, PraxisEquipmentRight equipmentRight)
    {
        var faultReports = _repository.GetItems<CirsGenericReport>(r =>
                    r.MetaData != null &&
                    r.MetaData["EquipmentId"] as string == equipmentId &&
                    !r.IsMarkedToDelete).ToList();
        var updates = new Dictionary<string, object>
        {
            { nameof(CirsGenericReport.EquipmentManagers), equipmentRight?.AssignedAdmins?.Select(u => u.UserId).ToList() ?? new List<string>()}
        };
        var filter = Builders<BsonDocument>.Filter.In("_id", faultReports.Select(r => r.ItemId).ToList());
        await _changeLogService.UpdateChange(nameof(CirsGenericReport), filter, updates);
        
        foreach (var faultReport in faultReports)
        {
            faultReport.EquipmentManagers = equipmentRight?.AssignedAdmins?.Select(u => u.UserId).ToList() ?? new List<string>();
            _cirsPermissionService.SetCirsReportPermission(faultReport);
        }
    }
}