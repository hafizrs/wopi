using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class DmsArtifactReapprovalEventHandlerService : IDmsArtifactReapprovalEventHandlerService
    {
        private readonly ILogger<DmsArtifactReapprovalEventHandlerService> _logger;
        private readonly IRepository _repository;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        public DmsArtifactReapprovalEventHandlerService(
            ILogger<DmsArtifactReapprovalEventHandlerService> logger,
            IRepository repository,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService)
        {
            _logger = logger;
            _repository = repository;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
        }

        public async Task InitiateArtifactReapprovalEventHandler()
        {
            _logger.LogInformation("Entered Service: {ServiceName}.", nameof(DmsArtifactReapprovalEventHandlerService));
            try
            {
                var nonDeletedArtifacts = _repository
                    .GetItems<ObjectArtifact>(artifact => !artifact.IsMarkedToDelete && artifact.OrganizationId != null)?
                    .ToList() ?? new List<ObjectArtifact>();
                
                var filteredArtifacts = nonDeletedArtifacts
                    .Where(artifact =>
                        _objectArtifactAuthorizationCheckerService.IsReapproveProcessStarted(artifact.MetaData))
                    .ToList();

                // Group artifacts by OrganizationId and select ItemIds
                var groupedArtifacts = filteredArtifacts
                    .GroupBy(artifact => artifact.OrganizationId)
                    .Select(group => new
                    {
                        OrganizationId = group.Key,
                        ItemIds = group.Select(artifact => artifact.ItemId).ToArray()
                    });

                // Process each group asynchronously
                foreach (var group in groupedArtifacts)
                {
                    await _cockpitDocumentActivityMetricsGenerationService
                        .OnDocumentReapproveGenerateObjectArtifactSummary(group.ItemIds);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Error occured while handling event: {ServiceName}. Error Message: {Message}.    Error Details: {StackTrace}",
                    nameof(DmsArtifactReapprovalEventHandlerService), e.Message, e.StackTrace);
            }
        }
    }
}
