using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;

public class DeleteTaskScheduleDataForPraxisProcessGuide : IDeleteTaskScheduleDataStrategy
{
    private readonly ILogger<DeleteTaskScheduleDataForPraxisProcessGuide> _logger;
    private readonly IRepository _repository;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
    private readonly IDependencyManagementService _dependencyManagementService;
    private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;

    public DeleteTaskScheduleDataForPraxisProcessGuide(
        ILogger<DeleteTaskScheduleDataForPraxisProcessGuide> logger,
        IRepository repository,
        ISecurityContextProvider securityContextProvider,
        ICockpitSummaryCommandService cockpitSummaryCommandService,
        IDependencyManagementService dependencyManagementService,
        ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService)
    {
        _logger = logger;
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
        _dependencyManagementService = dependencyManagementService;
        _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
    }

    public async Task<bool> DeleteTask(List<string> itemIds, TaskScheduleRemoveType removeType)
    {
        _logger.LogInformation("Enter {HandlerName} with ItemIds: {ItemIds}.", nameof(DeleteTaskScheduleDataForPraxisProcessGuide), JsonConvert.SerializeObject(itemIds));
        try
        {
            var guides = _repository
                .GetItems<PraxisProcessGuide>(pg => itemIds.Contains(pg.ItemId))
                .ToList();
            if (guides.Count != 0)
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var updates = new Dictionary<string, object>
                {
                    { nameof(PraxisProcessGuide.LastUpdatedBy), securityContext.UserId },
                    { nameof(PraxisProcessGuide.LastUpdateDate), DateTime.UtcNow },
                    { nameof(PraxisProcessGuide.IsMarkedToDelete), true }
                };

                var templateGuideIds = guides
                    .Where(g => g.IsATemplate)
                    .Select(g => g.ItemId)
                    .ToList();
                var clonedGuides = _repository.GetItems<PraxisProcessGuide>(pg => 
                        !pg.IsMarkedToDelete &&
                        pg.IsAClonedProcessGuide && 
                        templateGuideIds.Contains(pg.StandardTemplateId) &&
                        !itemIds.Contains(pg.ItemId))
                    .ToList();

                if (templateGuideIds.Count > 0)
                {
                    var templateConfigIds = guides
                        .Where(pg => pg.IsATemplate)
                        .Select(g => g.PraxisProcessGuideConfigId)
                        .ToList();
                    await _repository.UpdateManyAsync<PraxisProcessGuide>(pg => templateGuideIds.Contains(pg.ItemId), updates);
                    await _repository.UpdateManyAsync<PraxisProcessGuideConfig>(con => templateConfigIds.Contains(con.ItemId), updates);
                    await _repository.UpdateManyAsync<AssignedTaskForm>(t => templateGuideIds.Contains(t.AssignedEntityId), updates);
                }

                // general guides and cloned guides follows similar mechanism
                // filtered out template guides. following actions will be applied on general and cloned guides

                guides = guides
                    .Union(clonedGuides)
                    .Where(g => !g.IsATemplate)
                    .ToList();

                var taskSummaryIds = guides
                    .Where(g => !string.IsNullOrEmpty(g.TaskSchedule?.TaskSummaryId))
                    .Select(g => g.TaskSchedule.TaskSummaryId)
                    .ToList();
                var processGuideIds = guides.Select(g => g.ItemId).ToList();
                var configIds = guides.Select(g => g.PraxisProcessGuideConfigId).ToList();

                await _repository.UpdateManyAsync<PraxisProcessGuide>(p => processGuideIds.Contains(p.ItemId), updates);
                await _repository.UpdateManyAsync<TaskSummary>(t => taskSummaryIds.Contains(t.ItemId), updates);
                await _repository.UpdateManyAsync<TaskSchedule>(t => taskSummaryIds.Contains(t.TaskSummaryId), updates);
                await _repository.UpdateManyAsync<PraxisProcessGuideAnswer>(t => processGuideIds.Contains(t.ProcessGuideId), updates);
                await _repository.UpdateManyAsync<PraxisProcessGuideConfig>(t => configIds.Contains(t.ItemId), updates);
                await _repository.UpdateManyAsync<AssignedTaskForm>(t => processGuideIds.Contains(t.AssignedEntityId), updates);

                _cockpitSummaryCommandService.DeleteCockpitSummaryByTaskSummaryId(taskSummaryIds.ToArray(), nameof(PraxisProcessGuide));
                await _cockpitFormDocumentActivityMetricsGenerationService.OnDeleteTaskRemoveSummaryFromActivityMetrics(processGuideIds, nameof(PraxisProcessGuide));
                await _dependencyManagementService.HandleGuideDeletionAsync(processGuideIds);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in {HandlerName} with ItemIds: {ItemIds}.", nameof(DeleteTaskScheduleDataForPraxisProcessGuide), JsonConvert.SerializeObject(itemIds));
            return false;
        }
    }
}