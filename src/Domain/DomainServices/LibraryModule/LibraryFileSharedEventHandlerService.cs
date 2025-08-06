using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryFileSharedEventHandlerService : ILibraryFileSharedEventHandlerService
    {
        private readonly ILogger<LibraryFileSharedEventHandlerService> _logger;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ILibraryDocumentAssigneeService _libraryDocumentAssigneeService;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IObjectArtifactSharedDataResponseGeneratorService _objectArtifactSharedDataResponseGeneratorService;

        public LibraryFileSharedEventHandlerService(
            ILogger<LibraryFileSharedEventHandlerService> logger,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ILibraryDocumentAssigneeService libraryDocumentAssigneeService,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IObjectArtifactSharedDataResponseGeneratorService objectArtifactSharedDataResponseGeneratorService)
        {
            _logger = logger;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _libraryDocumentAssigneeService = libraryDocumentAssigneeService;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _objectArtifactSharedDataResponseGeneratorService = objectArtifactSharedDataResponseGeneratorService;
        }

        public async Task<bool> HandleLibraryFileSharedEvent(ObjectArtifactFileShareCommand command)
        {
            var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(command.ObjectArtifactId);
            if (objectArtifact == null) {
                _logger.LogWarning("ObjectArtifact with ID {ObjectArtifactId} not found.", command.ObjectArtifactId);
                return false;
            }
            if (command.NotifyToCockpit)
            {
                var activityName =  $"{CockpitDocumentActivityEnum.DOCUMENTS_ASSIGNED}";
                await _cockpitDocumentActivityMetricsGenerationService.OnDocumentShareGenerateActivityMetrics(
                    new[] { command.ObjectArtifactId }, activityName);
            }
            await DropInvalidActivityMetrics(objectArtifact);
            return true;
        }

        private async Task DropInvalidActivityMetrics(ObjectArtifact objectArtifact)
        {
            try
            {
                var assignedDepartments = await _libraryDocumentAssigneeService.GetPurposeWiseLibraryAssignees(
                    new LibraryDocumentAssigneeQuery
                    {
                        ObjectArtifactId = objectArtifact.ItemId,
                        Purpose = LibraryAssignedMemberType.ASSIGNED_TO
                    });
                if (assignedDepartments == null || !assignedDepartments.Any())
                {
                    _logger.LogInformation("No assigned departments found for object artifact {ObjectArtifactId}.", objectArtifact.ItemId);
                    return;
                }

                var summaries = _repository.GetItems<CockpitObjectArtifactSummary>(c =>
                        c.ObjectArtifactId == objectArtifact.ItemId &&
                        !c.IsMarkedToDelete && c.IsActive)
                    ?.Select(c => c.ItemId)
                    ?.ToList() ?? new List<string>();

                if (summaries == null || !summaries.Any())
                {
                    _logger.LogInformation("No summaries found for object artifact {ObjectArtifactId}.", objectArtifact.ItemId);
                    return;
                }
                var activityMetricsToBeDropped = new List<string>();
                var usersInActivityMetrics = new HashSet<string>();
                
                foreach (var d in assignedDepartments)
                {
                    var clientId = d.Id;
                    var assignees = d.Assignees?.Select(a => a.Id).ToList() ?? new List<string>();
                    var activityKey = nameof(CockpitDocumentActivityEnum.DOCUMENTS_ASSIGNED);
                    var matchedMetrics = _repository.GetItems<CockpitDocumentActivityMetrics>(c =>
                            c.ActivityKey == activityKey &&
                            c.DepartmentId == clientId &&
                            c.CockpitObjectArtifactSummaryIds.Any(d => summaries.Contains(d)))
                        ?.ToList() ?? new List<CockpitDocumentActivityMetrics>();
                    var droppedMetrics = matchedMetrics
                        .Where(m => !assignees.Contains(m.PraxisUserId))
                        .Select(m => m.ItemId)
                        .ToList();
                    if (droppedMetrics.Any())
                        activityMetricsToBeDropped.AddRange(droppedMetrics);
                    
                    var activeUsers = matchedMetrics
                        .Where(m => assignees.Contains(m.PraxisUserId))
                        .Select(m => m.PraxisUserId)
                        .ToHashSet();
                    if (activeUsers.Any())
                        usersInActivityMetrics.UnionWith(activeUsers);
                }
                if (activityMetricsToBeDropped.Any())
                {
                    _logger.LogInformation("Dropping {Count} invalid activity metrics with ids: {MetricsIds} for object artifact {ObjectArtifactId}.",
                        activityMetricsToBeDropped.Count, JsonConvert.SerializeObject(activityMetricsToBeDropped), objectArtifact.ItemId);
                    var dataUpdates = new Dictionary<string, object>
                    {
                        { nameof(CockpitDocumentActivityMetrics.IsMarkedToDelete), true },
                        { nameof(CockpitDocumentActivityMetrics.LastUpdateDate), DateTime.UtcNow },
                        { nameof(CockpitDocumentActivityMetrics.LastUpdatedBy), _securityContextProvider.GetSecurityContext().UserId }
                    };

                    await _repository.UpdateManyAsync<CockpitDocumentActivityMetrics>(u => activityMetricsToBeDropped.Contains(u.ItemId), dataUpdates);
                }
                else
                {
                    _logger.LogInformation("No invalid activity metrics found for object artifact {ObjectArtifactId}.",
                        objectArtifact.ItemId);
                }

                // Todo: Need to adjust the assignee details of objectartifact summaries
                var assigneeDetails = _objectArtifactSharedDataResponseGeneratorService.GetObjectArtifactAssigneeDetailResponse(objectArtifact);
                
                if (assigneeDetails == null)
                {
                    _logger.LogInformation("No assignee details found for object artifact {ObjectArtifactId}.", objectArtifact.ItemId);
                    return;
                }

                var updates = new Dictionary<string, object>
                {
                    { nameof(CockpitObjectArtifactSummary.LastUpdateDate), DateTime.UtcNow },
                    { nameof(CockpitObjectArtifactSummary.LastUpdatedBy), _securityContextProvider.GetSecurityContext().UserId },
                    { nameof(CockpitObjectArtifactSummary.AssigneeDetail), assigneeDetails.ToBsonDocument() }
                };

                await _repository.UpdateManyAsync<CockpitObjectArtifactSummary>(u => summaries.Contains(u.ItemId), updates);

            }
            catch (Exception ex)
            {
                _logger.LogWarning("Exception in {MethodName}. Message: {Message}", nameof(DropInvalidActivityMetrics), ex.Message);
            }
        }
    }
}