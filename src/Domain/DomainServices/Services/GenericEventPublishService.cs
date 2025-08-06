using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Linq;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

public class GenericEventPublishService : IGenericEventPublishService
{
    private readonly IServiceClient _serviceClient;
    private readonly IRepository _repository;

    public GenericEventPublishService(IServiceClient serviceClient, IRepository repository)
    {
        _serviceClient = serviceClient;
        _repository = repository;
    }

    public void PublishDmsArtifactUsageReferenceEvent(EntityBase entity)
    {
        switch (entity)
        {
            case PraxisForm praxisForm:
                PublishDmsArtifactUsageReferenceEventForPraxisForm(praxisForm);
                break;
            case CirsGenericReport cirsGenericReport:
                PublishDmsArtifactUsageReferenceEventForCirsGenericReport(cirsGenericReport);
                break;
            case PraxisOpenItem praxisOpenItem:
                PublishDmsArtifactUsageReferenceEventForPraxisOpenItem(praxisOpenItem);
                break;
            case PraxisEquipment praxisEquipment:
                PublishDmsArtifactUsageReferenceEventForEquipment(praxisEquipment);
                break;
            case PraxisEquipmentMaintenance praxisEquipmentMaintenance:
                PublishDmsArtifactUsageReferenceEventForEquipmentMaintenance(praxisEquipmentMaintenance);
                break;
            case RiqsShift riqsShift:
                PublishDmsArtifactUsageReferenceEventForShift(riqsShift);
                break;
        }
    }
    public void PublishDmsArtifactUsageReferenceDeleteEvent(EntityBase entity)
    {
        switch (entity)
        {
            case PraxisForm praxisForm:
                PublishDmsArtifactUsageReferenceDeleteEventForPraxisForm(praxisForm);
                break;
            case PraxisEquipment praxisEquipment:
                PublishDmsArtifactUsageReferenceDeleteEventForPraxisEquipment(praxisEquipment);
                break;
            case PraxisEquipmentMaintenance praxisEquipmentMaintenance:
                PublishDmsArtifactUsageReferenceDeleteEventForPraxisEquipmentMaintenance(praxisEquipmentMaintenance);
                break;
            case PraxisOpenItem praxisOpenItem:
                PublishDmsArtifactUsageReferenceDeleteEventForPraxisOpenItem(praxisOpenItem);
                break;
            case ObjectArtifact objectArtifact:
                SendDmsArtifactUsageReferenceDeleteEventToQueue(new List<string> { objectArtifact.ItemId }, entity.ItemId);
                break;
            case RiqsShift riqsShift:
                SendDmsArtifactUsageReferenceDeleteEventToQueue(riqsShift.Files?.Select(f => f.DocumentId).ToList(), entity.ItemId);
                break;
        }
    }

    private void PublishDmsArtifactUsageReferenceEventForPraxisForm(PraxisForm form)
    {
        if (string.IsNullOrEmpty(form?.PurposeOfFormKey)) return ;

        var purposeFormKey = form.PurposeOfFormKey;
        var purposeFormKeysWithArtifacts = new[] { "process-guide", "training-module" };

        if (!purposeFormKeysWithArtifacts.Contains(purposeFormKey)) return ;
        var objectArtifactIds = GetObjectArtifactIdsAttachedWithForm(form) ?? new List<string>();
        if (!objectArtifactIds.Any()) return;

        var purposeEntityName = string.Equals("process-guide", purposeFormKey, StringComparison.InvariantCultureIgnoreCase)
                ? EntityName.PraxisProcessGuide
                : EntityName.PraxisTraining;
        var metaData = new Dictionary<string, MetaValuePair>
        {
            { "SectorName", new MetaValuePair { Type = nameof(String), Value = form.Title } }
        };
        
        SendDmsArtifactUsageReferenceCreateEventToQueue(form.Description, objectArtifactIds, form.ItemId, EntityName.PraxisForm, purposeEntityName, metaData, form.ClientInfos?.ToList(), null, form.OrganizationIds);
    }

    private void PublishDmsArtifactUsageReferenceEventForCirsGenericReport(CirsGenericReport report)
    {
        var objectArtifactIds = new List<string>();

        if (report.AttachedDocuments?.Count > 0)
        {
            objectArtifactIds.AddRange(report.AttachedDocuments.Select(d => d.ItemId));
        }
        if (report.AttachedForm != null)
        {
            objectArtifactIds.Add(report.AttachedForm.ItemId);
        }
        if (objectArtifactIds.Count == 0) return;

        var metaData = new Dictionary<string, MetaValuePair>
        {
            { "CirsDashboardName", new MetaValuePair 
                { 
                    Type = nameof(String), 
                    Value = report.CirsDashboardName.ToString()
                }
            }
        };

        var clientInfos = 
            GetClientInfo(report.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId ?? string.Empty);

        SendDmsArtifactUsageReferenceCreateEventToQueue(report.Title, objectArtifactIds, report.ItemId, 
            EntityName.CirsGenericReport, EntityName.CirsGenericReport, metaData, clientInfos, report.OrganizationId, null,
            report.DueDate, report.Status);
    }
    private void PublishDmsArtifactUsageReferenceEventForEquipment(PraxisEquipment equipment)
    {
        var objectArtifactIds = equipment.Files?
            .Select(f => f.DocumentId)
            .ToList() ?? new List<string>();
        if (objectArtifactIds.Count == 0) return;

        var metaData = new Dictionary<string, MetaValuePair>();

        SendDmsArtifactUsageReferenceCreateEventToQueue(equipment.Name, objectArtifactIds, equipment.ItemId, 
            EntityName.PraxisEquipment, EntityName.PraxisEquipment, metaData, GetClientInfo(equipment.ClientId));
    }
    private void PublishDmsArtifactUsageReferenceEventForEquipmentMaintenance(PraxisEquipmentMaintenance maintenance)
    {
        if (maintenance.LibraryForms is not { Count: > 0 }) return;
        
        var libraryFormIds = maintenance.LibraryForms?
            .Select(lf => lf.LibraryFormId)
            .ToList() ?? new List<string>();
        if (libraryFormIds.Count != 1) return;
        
        var entityName = string.Compare(maintenance.ScheduleType, "Maintenance", StringComparison.OrdinalIgnoreCase) == 0
            ? EntityName.PraxisEquipmentMaintenance
            : EntityName.PraxisEquipmentValidation;

        var metaData = new Dictionary<string, MetaValuePair>
        {
            { "ClientId", new MetaValuePair { Type = nameof(String), Value = maintenance.ClientId } }
        };

        SendDmsArtifactUsageReferenceCreateEventToQueue(maintenance.Title, libraryFormIds, maintenance.ItemId, entityName, entityName,
            metaData, GetClientInfo(maintenance.ClientId), null, null, null, maintenance.CompletionStatus.Value);
    }
    private void PublishDmsArtifactUsageReferenceEventForPraxisOpenItem(PraxisOpenItem openItem)
    {
        var objectArtifactIds = GetObjectArtifactIdsFromOpenItem(openItem);
        if (objectArtifactIds == null || objectArtifactIds.Count == 0) return;

        var metaData = new Dictionary<string, MetaValuePair>();

        SendDmsArtifactUsageReferenceCreateEventToQueue(openItem.Title, objectArtifactIds, openItem.ItemId, 
            EntityName.PraxisOpenItem, EntityName.PraxisOpenItem, metaData, GetClientInfo(openItem.ClientId), null, null,
            openItem.TaskSchedule?.ToDateTime, openItem.OverAllCompletionStatus?.Value);
    }
    private void PublishDmsArtifactUsageReferenceEventForShift(RiqsShift shift)
    {
        var objectArtifactIds = shift.Files?
            .Select(f => f.DocumentId)
            .ToList() ?? new List<string>();
        objectArtifactIds.AddRange(shift.LibraryForms?
            .Select(f => f.LibraryFormId)
            .ToList() ?? new List<string>());
        if (objectArtifactIds.Count == 0) return;

        var metaData = new Dictionary<string, MetaValuePair>();

        var clientInfos = GetClientInfo(shift.DepartmentId);

        SendDmsArtifactUsageReferenceCreateEventToQueue(shift.ShiftName, objectArtifactIds, shift.ItemId,
            EntityName.RiqsShift, EntityName.RiqsShift, metaData, clientInfos, shift.OrganizationId);
    }
    private List<FormSpecificClientInfo> GetClientInfo(string clientId)
    {
        var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId);
        if (client == null) return null;
        return new List<FormSpecificClientInfo>
        {
            new()
            {
                ClientId = client.ItemId,
                ClientName = client.ClientName
            }
        };
    }
    private void SendDmsArtifactUsageReferenceCreateEventToQueue(
        string title, 
        List<string> objectArtifactIds, 
        string relatedEntityId, 
        string relatedEntityName, 
        string purposeEntityName,
        Dictionary<string, MetaValuePair> metadata,
        List<FormSpecificClientInfo> clientInfos,
        string organizationId = null,
        List<string> organizationIds = null,
        DateTime? dueDate = null,
        string completionStatus = null)
    {
        var dmsArtifactUsageReferenceEvent = new GenericEvent
        {
            EventType = PraxisEventType.DmsArtifactUsageReferenceEvent,
            JsonPayload = JsonConvert.SerializeObject(new DmsArtifactUsageReferenceEventModel
            {
                Title = title,
                ObjectArtifactIds = objectArtifactIds,
                RelatedEntityId = relatedEntityId,
                RelatedEntityName = relatedEntityName,
                PurposeEntityName = purposeEntityName,
                DueDate = dueDate?.Date.AddDays(1).AddSeconds(-1),
                CompletionStatus = completionStatus,
                MetaData = metadata,
                ClientInfos = clientInfos,
                OrganizationId = organizationId,
                OrganizationIds = organizationIds
            })
        };
        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), dmsArtifactUsageReferenceEvent);
    }
    public void SendDmsArtifactUsageReferenceCreateEventToQueue(DmsArtifactUsageReferenceEventModel model)
    {
        var dmsArtifactUsageReferenceEvent = new GenericEvent
        {
            EventType = PraxisEventType.DmsArtifactUsageReferenceEvent,
            JsonPayload = JsonConvert.SerializeObject(model)
        };
        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), dmsArtifactUsageReferenceEvent);
    }

    private void PublishDmsArtifactUsageReferenceDeleteEventForPraxisForm(PraxisForm existingForm)
    {
        var objectArtifactIds = GetObjectArtifactIdsAttachedWithForm(existingForm);
        SendDmsArtifactUsageReferenceDeleteEventToQueue(objectArtifactIds, existingForm.ItemId);
    }
    private void PublishDmsArtifactUsageReferenceDeleteEventForPraxisEquipment(PraxisEquipment entity)
    {
        var objectArtifactIds = entity
            .Files?
            .Select(f => f.DocumentId)
            .ToList() ?? new List<string>();
        if (!objectArtifactIds.Any()) return;
        SendDmsArtifactUsageReferenceDeleteEventToQueue(objectArtifactIds, entity.ItemId);
    }
    private void PublishDmsArtifactUsageReferenceDeleteEventForPraxisEquipmentMaintenance(PraxisEquipmentMaintenance entity)
    {
        var objectArtifactIds = entity
            .LibraryForms?
            .Select(f => f.LibraryFormId)
            .ToList() ?? new List<string>();
        if (!objectArtifactIds.Any()) return;
        SendDmsArtifactUsageReferenceDeleteEventToQueue(objectArtifactIds, entity.ItemId);
    }
    private void PublishDmsArtifactUsageReferenceDeleteEventForPraxisOpenItem(PraxisOpenItem openItem)
    {
        var objectArtifactIds = GetObjectArtifactIdsFromOpenItem(openItem);
        if (objectArtifactIds == null || objectArtifactIds.Count == 0) return;
        SendDmsArtifactUsageReferenceDeleteEventToQueue(objectArtifactIds, openItem.ItemId);
    }

    private void SendDmsArtifactUsageReferenceDeleteEventToQueue(List<string> objectArtifactIds, string relatedEntityId)
    {
        if (objectArtifactIds?.Count == 0) return;
        var dmsArtifactUsageReferenceDeleteEvent = new GenericEvent
        {
            EventType = PraxisEventType.DmsArtifactUsageReferenceDeleteEvent,
            JsonPayload = JsonConvert.SerializeObject(new DmsArtifactUsageReferenceDeleteEventModel
            {
                ObjectArtifactIds = objectArtifactIds,
                RelatedEntityId = relatedEntityId
            })
        };
        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), dmsArtifactUsageReferenceDeleteEvent);
    }

    private List<string> GetObjectArtifactIdsFromOpenItem(PraxisOpenItem openItem)
    {
        List<string> objectArtifactIds;

        if (openItem.TaskReference?.Value != "Form")
        {
            objectArtifactIds = openItem.DocumentInfo?
                .Select(d => d.DocumentId)
                .ToList() ?? new List<string>();
        }
        else
        {
            objectArtifactIds = new List<string>
            {
                openItem.TaskReferenceId
            };
        }

        return objectArtifactIds;
    }

    private List<string> GetObjectArtifactIdsAttachedWithForm(PraxisForm formData)
    {
        var artifactIds = new List<string>();

        if (formData is null)
        {
            return artifactIds;
        }

        AddProcessGuideCheckListArtifactIds(formData, artifactIds);
        AddFormFilesArtifactIds(formData.Files, artifactIds);
        AddQuestionsListArtifactIds(formData, artifactIds);

        return artifactIds;
    }

    private void AddProcessGuideCheckListArtifactIds(PraxisForm formData, List<string> artifactIds)
    {
        if (formData.ProcessGuideCheckList != null)
        {
            foreach (var checkList in formData.ProcessGuideCheckList)
            {
                foreach (var processGuideTask in checkList.ProcessGuideTask ?? Enumerable.Empty<ProcessGuideTask>())
                {
                    AddFormFilesArtifactIds(processGuideTask.Files, artifactIds);
                    artifactIds.AddRange(processGuideTask.LibraryForms?
                        .Select(form => form.LibraryFormId)
                        .ToList() ?? Enumerable.Empty<string>());
                }
            }
        }
    }

    private void AddQuestionsListArtifactIds(PraxisForm formData, List<string> artifactIds)
    {
        if (formData.QuestionsList != null)
        {
            foreach (var praxisQuestion in formData.QuestionsList)
            {
                if (praxisQuestion.QuestionOptions != null)
                {
                    foreach (var question in praxisQuestion.QuestionOptions)
                    {
                        AddFormFilesArtifactIds(question.Files, artifactIds);
                    }
                }
                AddFormFilesArtifactIds(praxisQuestion.Files, artifactIds);
            }
        }
    }

    private void AddFormFilesArtifactIds(IEnumerable<PraxisDocument> files, List<string> artifactIds)
    {
        artifactIds.AddRange(files?
            .Select(document => document.DocumentId)
            .ToList() ?? Enumerable.Empty<string>());
    }

    #region Training_Answer
    public void PublishTrainingQualificationPassedEvent(string trainingId)
    {
        var trainingQualificationPassedEvent = new GenericEvent
        {
            EventType = PraxisEventType.PraxisTrainingQualificationPassedEvent,
            JsonPayload = trainingId
        };
        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), trainingQualificationPassedEvent);
    }


    #endregion
}