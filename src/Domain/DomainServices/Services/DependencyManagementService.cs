using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using System.Text.RegularExpressions;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

public class DependencyManagementService : IDependencyManagementService
{
    private readonly ILogger<DependencyManagementService> _logger;
    private readonly IRepository _repository;
    private readonly IReportingTaskCockpitSummaryCommandService _reportingTaskCockpitSummaryCommandService;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
    private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
    private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;

    public DependencyManagementService(
        ILogger<DependencyManagementService> logger,
        IRepository repository, 
        IReportingTaskCockpitSummaryCommandService reportingTaskCockpitSummaryCommandService,
        ICockpitSummaryCommandService cockpitSummaryCommandService,
        IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
        ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService)
    {
        _logger = logger;
        _repository = repository;
        _reportingTaskCockpitSummaryCommandService = reportingTaskCockpitSummaryCommandService;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
        _mongoDbDataContextProvider = mongoDbDataContextProvider;
        _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
    }

    #region public_methods
    public async Task HandleFileDeletionAsync(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered event handler: {EventHandlerName} with fileIds {FileIds}.",
            nameof(DependencyManagementService), JsonConvert.SerializeObject(fileIds));
        try
        {
            fileIds ??= Enumerable.Empty<string>();

            if (!fileIds.Any()) return;

            await DeleteFileFromEquipmentAndMaintenance(fileIds);
            await DeleteFileFromOpenItem(fileIds);
            await DeleteFileFromShift(fileIds);
            await DeleteFileFromShiftPlan(fileIds);
            await DeleteFileFromPraxisForm(fileIds);
            await DeleteFileFromReporting(fileIds);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during {EventType} event handle.", nameof(HandleFileDeletionAsync));
            _logger.LogError("Exception Message: {ExceptionMessage} Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }

    public async Task HandleFileInactivationAsync(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered event handler: {EventHandlerName} with fileIds {FileIds}.",
            nameof(DependencyManagementService), JsonConvert.SerializeObject(fileIds));
        try
        {
            fileIds ??= Enumerable.Empty<string>();
            if (!fileIds.Any()) return;
            await HandleFileDeletionAsync(fileIds);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during {EventType} event handle.", nameof(HandleFileInactivationAsync));
            _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }

    public async Task HandleGuideDeletionAsync(IEnumerable<string> guideIds)
    {
        _logger.LogInformation("Entered event handler: {EventHandlerName} with guideId {GuideId}.",
            nameof(DependencyManagementService), guideIds);
        try
        {
            guideIds ??= Enumerable.Empty<string>();

            if (!guideIds.Any()) return;

            var formIds = GetFormIdsByGuideIds(guideIds);

            await DeleteClonedProcessGuide(guideIds);
            await DeleteGuideFromReporting(guideIds);
            await DeleteGuideFromEquipmentAndMaintenance(formIds);
            await DeleteGuideFromShift(formIds);
            await DeleteGuideFromShiftPlan(formIds);
            await DeleteCockpitFormDocument(guideIds, nameof(PraxisProcessGuide));
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during {EventType} event handle.", nameof(HandleGuideDeletionAsync));
            _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }

    public async Task HandleGuideInactivationAsync(IEnumerable<string> guideIds)
    {
        _logger.LogInformation("Entered event handler: {EventHandlerName} with guideId {GuideId}.",
            nameof(DependencyManagementService), JsonConvert.SerializeObject(guideIds));
        try
        {
            guideIds ??= Enumerable.Empty<string>();
            if (!guideIds.Any()) return;
            await HandleGuideDeletionAsync(guideIds);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during {EventType} event handle.", nameof(HandleGuideInactivationAsync));
            _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", e.Message, e.StackTrace);

        }
    }

    public async Task HandleTodoDeletionAsync(IEnumerable<string> todoIds)
    {
        _logger.LogInformation("Entered event handler: {EventHandlerName} with todoIds {TodoIds}.",
            nameof(DependencyManagementService), JsonConvert.SerializeObject(todoIds));
        try
        {
            todoIds ??= Enumerable.Empty<string>();
            if (!todoIds.Any()) return;
            await DeleteTodoFromReporting(todoIds);
            await DeleteCockpitFormDocument(todoIds, nameof(PraxisOpenItem));
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during {EventType} event handle.", nameof(HandleTodoDeletionAsync));
            _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }

    public async Task HandleTodoInactivationAsync(IEnumerable<string> todoIds)
    {
        _logger.LogInformation("Entered event handler: {EventHandlerName} with todoIds {TodoIds}.",
            nameof(DependencyManagementService), JsonConvert.SerializeObject(todoIds));
        try
        {
            todoIds ??= Enumerable.Empty<string>();
            if (!todoIds.Any()) return;
            await HandleCockpitReportingDependentTaskInactivation(todoIds);
            await HandleTodoDeletionAsync(todoIds);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during {EventType} event handle.", nameof(HandleTodoInactivationAsync));
            _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }

    public async Task HandlePraxisFormDeletionAsync(IEnumerable<string> praxisFormIds)
    {
        _logger.LogInformation("Entered event handler: {EventHandlerName} with praxisFormIds {PraxisFormIds}.",
            nameof(DependencyManagementService), JsonConvert.SerializeObject(praxisFormIds));
        try
        {
            praxisFormIds ??= Enumerable.Empty<string>();

            if (!praxisFormIds.Any()) return;

            await DeleteGuideFromEquipmentAndMaintenance(praxisFormIds);
            await DeleteGuideFromShift(praxisFormIds);

            await DeleteDependentGuides(praxisFormIds);
            await DeleteDependentTrainings(praxisFormIds);
            await DeleteDependentControlTasks(praxisFormIds);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during {EventType} event handle.", nameof(HandlePraxisFormDeletionAsync));
            _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }
    #endregion

    #region Riqs-Pedia-Deletion
    private async Task DeleteFileFromEquipmentAndMaintenance(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteFileFromEquipmentAndMaintenance), fileIds);
        try
        {
            var equipments = _repository.GetItems<PraxisEquipment>(pe =>
                !pe.IsMarkedToDelete &&
                pe.Files != null &&
                pe.Files.Any(f => fileIds.Contains(f.DocumentId)));
            foreach (var praxisEquipment in equipments)
            {
                praxisEquipment.Files = praxisEquipment.Files.Where(f => !fileIds.Contains(f.DocumentId)).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisEquipment.ItemId, praxisEquipment);
            }

            var maintenances = _repository.GetItems<PraxisEquipmentMaintenance>(pm =>
                !pm.IsMarkedToDelete &&
                pm.LibraryForms != null &&
                pm.LibraryForms.Any(f => fileIds.Contains(f.LibraryFormId)));
            foreach (var praxisEquipmentMaintenance in maintenances)
            {
                praxisEquipmentMaintenance.LibraryForms = praxisEquipmentMaintenance.LibraryForms
                    .Where(f => !fileIds.Contains(f.LibraryFormId)).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisEquipmentMaintenance.ItemId, praxisEquipmentMaintenance);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteFileFromEquipmentAndMaintenance), JsonConvert.SerializeObject(fileIds), e.Message, e.StackTrace);
        }
    }

    private async Task DeleteFileFromOpenItem(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteFileFromOpenItem), JsonConvert.SerializeObject(fileIds));
        try
        {
            var openItems = _repository.GetItems<PraxisOpenItem>(po =>
                !po.IsMarkedToDelete &&
                po.DocumentInfo != null &&
                po.DocumentInfo.Any(f => fileIds.Contains(f.DocumentId)));
            foreach (var praxisOpenItem in openItems)
            {
                praxisOpenItem.DocumentInfo = praxisOpenItem.DocumentInfo.Where(f => !fileIds.Contains(f.DocumentId)).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisOpenItem.ItemId, praxisOpenItem);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteFileFromOpenItem), JsonConvert.SerializeObject(fileIds), e.Message, e.StackTrace);
        }
    }

    private async Task DeleteFileFromShift(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteFileFromShift), fileIds);
        try
        {
            var shifts = _repository.GetItems<RiqsShift>(ps =>
                !ps.IsMarkedToDelete &&
                ((ps.Files != null &&
                  ps.Files.Any(f => fileIds.Contains(f.DocumentId)) ||
                  (ps.LibraryForms != null &&
                   ps.LibraryForms.Any(f => fileIds.Contains(f.LibraryFormId))))));
            foreach (var praxisShift in shifts)
            {
                praxisShift.Files = praxisShift.Files?.Where(f => !fileIds.Contains(f.DocumentId)).ToList();
                praxisShift.LibraryForms = praxisShift.LibraryForms?.Where(f => !fileIds.Contains(f.LibraryFormId)).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisShift.ItemId, praxisShift);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteFileFromShift), JsonConvert.SerializeObject(fileIds), e.Message, e.StackTrace);
        }
    }

    private async Task DeleteFileFromShiftPlan(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteFileFromShiftPlan), fileIds);
        try
        {
            var startOfCurrentDay = DateTime.Now.Date;
            var shiftPlans = _repository.GetItems<RiqsShiftPlan>(ps =>
                !ps.IsMarkedToDelete &&
                ps.ShiftDate >= startOfCurrentDay &&
                ((ps.Shift.Files != null &&
                  ps.Shift.Files.Any(f => fileIds.Contains(f.DocumentId))) ||
                 (ps.Shift.LibraryForms != null &&
                  ps.Shift.LibraryForms.Any(f => fileIds.Contains(f.LibraryFormId)))));
            foreach (var praxisShiftPlan in shiftPlans)
            {
                praxisShiftPlan.Shift.Files = praxisShiftPlan.Shift.Files?
                    .Where(f => !fileIds.Contains(f.DocumentId)).ToList();
                praxisShiftPlan.Shift.LibraryForms = praxisShiftPlan.Shift.LibraryForms?
                    .Where(f => !fileIds.Contains(f.LibraryFormId)).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisShiftPlan.ItemId, praxisShiftPlan);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteFileFromShiftPlan), JsonConvert.SerializeObject(fileIds), e.Message, e.StackTrace);
        }
    }

    private async Task DeleteFileFromPraxisForm(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteFileFromPraxisForm), JsonConvert.SerializeObject(fileIds));
        try
        {
            var praxisForms = _repository.GetItems<PraxisForm>(pf =>
                !pf.IsMarkedToDelete &&
                ((pf.Files != null && pf.Files.Any(f => fileIds.Contains(f.DocumentId))) ||
                 (pf.ProcessGuideCheckList.Any(c =>
                     c.ProcessGuideTask.Any(t =>
                         t.Files != null && t.Files.Any(f => fileIds.Contains(f.DocumentId))))) ||
                 (pf.QuestionsList.Any(q => q.Files.Any(f => fileIds.Contains(f.DocumentId))))));
            foreach (var praxisForm in praxisForms)
            {
                praxisForm.Files = praxisForm.Files?.Where(f => !fileIds.Contains(f.DocumentId)).ToList();
                praxisForm.ProcessGuideCheckList = praxisForm.ProcessGuideCheckList?
                    .Select(c =>
                    {
                        c.ProcessGuideTask = c.ProcessGuideTask?
                            .Select(t =>
                            {
                                t.Files = t.Files?.Where(f => !fileIds.Contains(f.DocumentId)).ToList();
                                return t;
                            }).ToList();
                        return c;
                    }).ToList();
                praxisForm.QuestionsList = praxisForm.QuestionsList?
                    .Select(q =>
                    {
                        q.Files = q.Files?.Where(f => !fileIds.Contains(f.DocumentId)).ToList();
                        return q;
                    }).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisForm.ItemId, praxisForm);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteFileFromPraxisForm), JsonConvert.SerializeObject(fileIds), e.Message, e.StackTrace);
        }
    }

    private async Task DeleteFileFromReporting(IEnumerable<string> fileIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteFileFromReporting), JsonConvert.SerializeObject(fileIds));
        try
        {
            var reports = _repository.GetItems<CirsGenericReport>(pr =>
                !pr.IsMarkedToDelete &&
                ((pr.AttachmentIds != null && pr.AttachmentIds.Any(f => fileIds.Contains(f))) ||
                 (pr.AttachedDocuments != null &&
                  pr.AttachedDocuments.Any(f => fileIds.Contains(f.ItemId))) ||
                 (pr.AttachedForm != null && fileIds.Contains(pr.AttachedForm.ItemId))));
            foreach (var praxisReport in reports)
            {
                praxisReport.AttachmentIds = praxisReport.AttachmentIds?.Where(f => !fileIds.Contains(f)).ToList();
                praxisReport.AttachedDocuments = praxisReport.AttachedDocuments?.Where(f => !fileIds.Contains(f.ItemId)).ToList();
                if (fileIds.Contains(praxisReport.AttachedForm?.ItemId))
                {
                    praxisReport.AttachedForm = null;
                }
                await _repository.UpdateAsync(e => e.ItemId == praxisReport.ItemId, praxisReport);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteFileFromReporting), JsonConvert.SerializeObject(fileIds), e.Message, e.StackTrace);
        }
    }
    #endregion

    #region Guide-Deletion
    private async Task DeleteClonedProcessGuide(IEnumerable<string> guideIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteClonedProcessGuide), JsonConvert.SerializeObject(guideIds));
        try
        {
            if (!guideIds.Any()) return;
            var clonedGuides = _repository.GetItems<PraxisProcessGuide>(pg =>
                    !pg.IsMarkedToDelete &&
                    pg.IsAClonedProcessGuide &&
                    guideIds.Contains(pg.StandardTemplateId))
                .Select(g => g.ItemId)
                .ToList();
            if (!clonedGuides.Any()) return;
            var updates = new Dictionary<string, object>
            {
                { nameof(PraxisProcessGuide.IsMarkedToDelete), true }
            };
            await _repository.UpdateManyAsync<PraxisProcessGuide>(e => clonedGuides.Contains(e.ItemId), updates);
            await _cockpitSummaryCommandService.DeleteSummaryAsync(clonedGuides, CockpitTypeNameEnum.PraxisProcessGuide);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteClonedProcessGuide), JsonConvert.SerializeObject(guideIds), e.Message, e.StackTrace);
        }
    }
    private async Task DeleteGuideFromReporting(IEnumerable<string> guideIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteGuideFromReporting), JsonConvert.SerializeObject(guideIds));
        try
        {
            if (!guideIds.Any()) return;
            var reports = _repository.GetItems<CirsGenericReport>(pr =>
                !pr.IsMarkedToDelete &&
                pr.ProcessGuideAttachments != null &&
                pr.ProcessGuideAttachments.Any(f => guideIds.Contains(f.ProcessGuideId)));
            foreach (var praxisReport in reports)
            {
                var currentGuideIds = praxisReport.ProcessGuideAttachments?
                    .Where(f => guideIds.Contains(f.ProcessGuideId))
                    .Select(f => f.ProcessGuideId) ?? Enumerable.Empty<string>();

                await Task.WhenAll(currentGuideIds.Select(guideId =>
                    _reportingTaskCockpitSummaryCommandService.OnProcessGuideDeletionUpdateSummary(guideId)));


                praxisReport.ProcessGuideAttachments = praxisReport.ProcessGuideAttachments?
                    .Where(f => !guideIds.Contains(f.ProcessGuideId)).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisReport.ItemId, praxisReport);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteGuideFromReporting), JsonConvert.SerializeObject(guideIds), e.Message, e.StackTrace);
        }
    }
    private async Task DeleteGuideFromEquipmentAndMaintenance(IEnumerable<string> formIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteGuideFromEquipmentAndMaintenance), JsonConvert.SerializeObject(formIds));
        try
        {
            if (!formIds.Any()) return;
            var processGuideListingKey = "ProcessGuideListing";
            var regexPattern = string.Join("|", formIds.Select(Regex.Escape)); // Combine all formIds into a single regex pattern
            var filter = new BsonDocument
            {
                { "IsMarkedToDelete", false },
                { "MetaValues", new BsonDocument("$elemMatch", new BsonDocument
                    {
                        { "Key", processGuideListingKey },
                        { "Value", new BsonDocument("$regex", regexPattern) } // Check if 'Value' contains any of the formIds
                    })
                }
            };
            var projection = Builders<PraxisEquipment>.Projection
                .Include(e => e.ItemId)
                .Include(e => e.MetaValues);
            var collection = _mongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<PraxisEquipment>($"{nameof(PraxisEquipment)}s");
            var documents = await collection
                .Find(filter)
                .Project(projection)
                .ToListAsync();
            var equipments = documents?.Select(d => new PraxisEquipment
            {
                ItemId = d.GetValue("_id").AsString,
                MetaValues = d["MetaValues"]?.AsBsonArray?
                    .Select(x => BsonSerializer.Deserialize<PraxisKeyValue>(x.AsBsonDocument))
                    .ToList() ?? new List<PraxisKeyValue>()
            }).ToList() ?? new List<PraxisEquipment>() ;

            foreach (var equipment in equipments)
            {
                var existingProcessGuideListing = new List<EquipmentProcessGuideListing>();
                if (equipment.MetaValues?.Any() ?? false)
                {
                    existingProcessGuideListing = JsonConvert.DeserializeObject<List<EquipmentProcessGuideListing>>(
                        equipment.MetaValues.FirstOrDefault(meta => meta.Key.Equals(processGuideListingKey))?.Value ?? "[]"
                    );
                }
                existingProcessGuideListing.RemoveAll(f => formIds.Contains(f.FormId));
                var updatedMetaValues = (equipment.MetaValues ?? Enumerable.Empty<PraxisKeyValue>())
                    .Where(meta => !meta.Key.Equals(processGuideListingKey)) // Remove old key
                    .ToList();
                updatedMetaValues.Add(new PraxisKeyValue
                {
                    Key = processGuideListingKey,
                    Value = JsonConvert.SerializeObject(existingProcessGuideListing)
                });

                var localUpdates = new Dictionary<string, object>()
                {
                    {
                        nameof(PraxisEquipment.MetaValues),
                        updatedMetaValues.Select(meta => new BsonDocument
                        {
                            { nameof(PraxisKeyValue.Key), meta.Key },
                            { nameof(PraxisKeyValue.Value), meta.Value }
                        }).ToList()
                    }
                };

                //await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, localUpdates);
                Expression<Func<PraxisEquipment, bool>> dataFilters = e => e.ItemId == equipment.ItemId;
                await _repository.UpdateAsync(dataFilters, localUpdates);
            }

            var maintenances = _repository.GetItems<PraxisEquipmentMaintenance>(pm =>
                    !pm.IsMarkedToDelete &&
                    !(pm.CompletionStatus != null &&
                      pm.CompletionStatus.Value == "DONE") &&
                    pm.PraxisFormInfo != null &&
                    formIds.Contains(pm.PraxisFormInfo.FormId))
                .Select(m => m.ItemId)
                .ToList();
            if (!maintenances.Any()) return;
            var updates = new Dictionary<string, object>
            {
                { nameof(PraxisEquipmentMaintenance.PraxisFormInfo), null }
            };
            await _repository.UpdateManyAsync<PraxisEquipmentMaintenance>(
                e => maintenances.Contains(e.ItemId), updates);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteGuideFromEquipmentAndMaintenance), JsonConvert.SerializeObject(formIds), e.Message, e.StackTrace);
        }
    }
    private async Task DeleteGuideFromShift(IEnumerable<string> formIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteGuideFromShift), JsonConvert.SerializeObject(formIds));
        try
        {
            if (!formIds.Any()) return;
            var shifts = _repository.GetItems<RiqsShift>(ps =>
                !ps.IsMarkedToDelete &&
                ps.PraxisFormIds != null &&
                formIds.Any(f => ps.PraxisFormIds.Contains(f)));
            foreach (var praxisShift in shifts)
            {
                praxisShift.PraxisFormIds.RemoveAll(f => formIds.Contains(f));
                await _repository.UpdateAsync(e => e.ItemId == praxisShift.ItemId, praxisShift);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteGuideFromShift), JsonConvert.SerializeObject(formIds), e.Message, e.StackTrace);
        }
    }
    private async Task DeleteGuideFromShiftPlan(IEnumerable<string> formIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteGuideFromShiftPlan), JsonConvert.SerializeObject(formIds));
        try
        {
            if (!formIds.Any()) return;
            var startOfCurrentDate = DateTime.Now.Date;
            var shiftPlans = _repository.GetItems<RiqsShiftPlan>(ps =>
                    !ps.IsMarkedToDelete &&
                    ps.ShiftDate >= startOfCurrentDate &&
                    ps.Shift != null &&
                    ps.Shift.PraxisFormIds != null &&
                    formIds.Any(f => ps.Shift.PraxisFormIds.Contains(f)));
            foreach (var praxisShiftPlan in shiftPlans)
            {
                praxisShiftPlan.Shift.PraxisFormIds.RemoveAll(f => formIds.Contains(f));
                await _repository.UpdateAsync(e => e.ItemId == praxisShiftPlan.ItemId, praxisShiftPlan);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteGuideFromShiftPlan), JsonConvert.SerializeObject(formIds), e.Message, e.StackTrace);
        }
    }

    private async Task DeleteCockpitFormDocument(IEnumerable<string> itemIds, string entityName)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload} & EntityName: {EntityName}.", 
            nameof(DeleteCockpitFormDocument), JsonConvert.SerializeObject(itemIds), entityName);
        try
        {
            if (!itemIds.Any()) return;
            await _cockpitFormDocumentActivityMetricsGenerationService.OnDeleteTaskRemoveSummaryFromActivityMetrics(itemIds.ToList(), entityName);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteCockpitFormDocument), JsonConvert.SerializeObject(itemIds), e.Message, e.StackTrace);
        }
    }

    private List<string> GetFormIdsByGuideIds(IEnumerable<string> guideIds)
    {
        if (guideIds == null || !guideIds.Any()) return new List<string>();
        var formIds = _repository.GetItems<PraxisProcessGuide>(pf =>
                !pf.IsMarkedToDelete &&
                guideIds.Contains(pf.ItemId))
            .Select(p => p.FormId)
            .ToList();
        return formIds;
    }
    #endregion

    #region Todo-Delition
    private async Task DeleteTodoFromReporting(IEnumerable<string> todoIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteTodoFromReporting), JsonConvert.SerializeObject(todoIds));
        try
        {
            var reporting = _repository.GetItems<CirsGenericReport>(pr =>
                !pr.IsMarkedToDelete &&
                pr.IsActive &&
                pr.Status != "Completed" &&
                pr.OpenItemAttachments != null &&
                pr.OpenItemAttachments.Any(f => todoIds.Contains(f.OpenItemId)));
            foreach (var praxisReport in reporting)
            {
                var currentTodoIds = praxisReport.OpenItemAttachments?
                    .Where(f => todoIds.Contains(f.OpenItemId))
                    .Select(f => f.OpenItemId) ?? Enumerable.Empty<string>();
                await Task.WhenAll(currentTodoIds.Select(async todoId =>
                    await _reportingTaskCockpitSummaryCommandService.OnOpenItemDeletionUpdateSummary(todoId)));
                praxisReport.OpenItemAttachments = praxisReport.OpenItemAttachments?
                    .Where(f => !todoIds.Contains(f.OpenItemId)).ToList();
                await _repository.UpdateAsync(e => e.ItemId == praxisReport.ItemId, praxisReport);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteTodoFromReporting), JsonConvert.SerializeObject(todoIds), e.Message, e.StackTrace);
        }
    }

    #endregion

    #region Todo-Inactivation

    private async Task HandleCockpitReportingDependentTaskInactivation(IEnumerable<string> itemIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(HandleCockpitReportingDependentTaskInactivation), JsonConvert.SerializeObject(itemIds));
        try
        {
            var summaries = _repository.GetItems<RiqsTaskCockpitSummary>(cs =>
                !cs.IsMarkedToDelete &&
                !cs.IsTaskCompleted &&
                !cs.IsSummaryHidden &&
                cs.DependentTasks != null &&
                cs.DependentTasks.Any(f => itemIds.Contains(f.TaskId)));
            foreach (var summary in summaries)
            {
                foreach (var dependentTask in summary.DependentTasks)
                {
                    if (itemIds.Contains(dependentTask.TaskId))
                    {
                        dependentTask.TaskStatus = false;
                    }
                }
                await _repository.UpdateAsync<RiqsTaskCockpitSummary>(r => r.ItemId == summary.ItemId, summary);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(HandleCockpitReportingDependentTaskInactivation), JsonConvert.SerializeObject(itemIds), e.Message, e.StackTrace);
        }
    }

    #endregion

    #region Praxis-Form-Deletion
    private async Task DeleteDependentGuides(IEnumerable<string> praxisFormIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteDependentGuides), JsonConvert.SerializeObject(praxisFormIds));
        try
        {
            var guideIds = _repository.GetItems<AssignedTaskForm>(pg =>
                    !pg.IsMarkedToDelete &&
                    pg.ClonedFormId != null &&
                    praxisFormIds.Contains(pg.ClonedFormId))
                .Select(g => g.AssignedEntityId)
                .ToList();
            if (!guideIds.Any()) return;
            await HandleGuideDeletionAsync(guideIds);
            var updates = new Dictionary<string, object>
            {
                { nameof(PraxisProcessGuide.IsMarkedToDelete), true }
            };
            await _repository.UpdateManyAsync<PraxisProcessGuide>(
                e => guideIds.Contains(e.ItemId), updates);
            await _cockpitSummaryCommandService
                .DeleteSummaryAsync(guideIds, CockpitTypeNameEnum.PraxisProcessGuide);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteDependentGuides), JsonConvert.SerializeObject(praxisFormIds), e.Message, e.StackTrace);
        }
    }
    private async Task DeleteDependentTrainings(IEnumerable<string> praxisFormIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteDependentTrainings), JsonConvert.SerializeObject(praxisFormIds));
        try
        {
            var trainings = _repository.GetItems<PraxisTraining>(pt =>
                    !pt.IsMarkedToDelete &&
                    pt.FormId != null &&
                    praxisFormIds.Contains(pt.FormId))
                .Select(t => t.ItemId)
                .ToList();
            if (!trainings.Any()) return;
            var updates = new Dictionary<string, object>
            {
                { nameof(PraxisTraining.IsMarkedToDelete), true }
            };
            await _repository.UpdateManyAsync<PraxisTraining>(
                e => trainings.Contains(e.ItemId), updates);
            await _cockpitSummaryCommandService
                .DeleteSummaryAsync(trainings, CockpitTypeNameEnum.PraxisTraining);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteDependentTrainings), JsonConvert.SerializeObject(praxisFormIds), e.Message, e.StackTrace);
        }
    }
    private async Task DeleteDependentControlTasks(IEnumerable<string> praxisFormIds)
    {
        _logger.LogInformation("Entered {Method} with payload: {Payload}.", nameof(DeleteDependentControlTasks), JsonConvert.SerializeObject(praxisFormIds));
        try
        {
            var taskConfigIds = _repository.GetItems<PraxisTaskConfig>(pt =>
                    !pt.IsMarkedToDelete &&
                    pt.FormIds != null &&
                    praxisFormIds.Any(f => pt.FormIds.Contains(f)))
                .Select(t => t.ItemId)
                .ToList();
            if (!taskConfigIds.Any()) return;
            var taskIds = _repository.GetItems<PraxisTask>(pt =>
                    !pt.IsMarkedToDelete &&
                    pt.TaskConfigId != null &&
                    taskConfigIds.Contains(pt.TaskConfigId))
                .Select(t => t.ItemId)
                .ToList();
            if (!taskIds.Any()) return;
            var updates = new Dictionary<string, object>
            {
                { nameof(PraxisTask.IsMarkedToDelete), true }
            };
            await _repository.UpdateManyAsync<PraxisTask>(
                e => taskIds.Contains(e.ItemId), updates);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {Method} with payload: {Payload}. Message: {Error}. StackTrace: {StackTrace}",
                nameof(DeleteDependentControlTasks), JsonConvert.SerializeObject(praxisFormIds), e.Message, e.StackTrace);
        }
    }

    #endregion
}