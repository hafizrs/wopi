using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public class DmsArtifactUsageReferenceQueryService : IDmsArtifactUsageReferenceQueryService
    {
        private readonly ILogger<DmsArtifactUsageReferenceQueryService> _logger;
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ISecurityHelperService _securityHelperService;

        public DmsArtifactUsageReferenceQueryService(
            ILogger<DmsArtifactUsageReferenceQueryService> logger,
            IRepository repository,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ISecurityHelperService securityHelperService)
        {
            _logger = logger;
            _repository = repository;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _securityHelperService = securityHelperService;
        }
        public Task<List<DmsArtifactUsageReferenceDto>> GetDmsArtifactUsageReference(string objectArtifactId, string clientId)
        {
            _logger.LogInformation("Entered into {HandlerName} with Query: {Query}",
                nameof(DmsArtifactUsageReferenceQueryService), objectArtifactId);
            try
            {
                var isDepartmentLevelUser = _securityHelperService.IsADepartmentLevelUser();
                if (isDepartmentLevelUser)
                {
                    ArgumentNullException.ThrowIfNull(clientId, nameof(clientId));
                }

                var builder = Builders<DmsArtifactUsageReference>.Filter;
                var filter = builder.Eq(artifact => artifact.ObjectArtifactId, objectArtifactId) &
                             builder.Ne(artifact => artifact.IsMarkedToDelete, true);
                
                if (isDepartmentLevelUser)
                {
                    var orgId = _securityHelperService.ExtractOrganizationFromOrgLevelUser();
                    var orfilters = new List<FilterDefinition<DmsArtifactUsageReference>>
                    {
                        builder.Exists(artifact => artifact.ClientInfos) &
                              builder.ElemMatch(artifact => artifact.ClientInfos, clientInfo => clientInfo.ClientId == clientId),
                        builder.Eq(artifact => artifact.OrganizationId, orgId),
                        builder.AnyEq(artifact => artifact.OrganizationIds, orgId),
                    };
                    filter &= builder.Or(orfilters);
                }

                var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext()
                    .GetCollection<DmsArtifactUsageReference>($"{nameof(DmsArtifactUsageReference)}s");
                var result = collection
                    .Find(filter)
                    .ToList() ?? new List<DmsArtifactUsageReference>();

                var groupedResult = result
                    .GroupBy(model => model.RelatedEntityName)
                    .Select(group => new DmsArtifactUsageReferenceDto
                    {
                        RelatedEntityName = group.Key,
                        Data = group
                            .Select(artifact => new DmsArtifactUsageReferenceDtoModel(artifact))
                            .ToList(),
                        TotalCount = group.Count()
                    })
                    .ToList();
                
                _logger.LogInformation("Handled by {HandlerName}", nameof(DmsArtifactUsageReferenceQueryService));
                
                return Task.FromResult(groupedResult);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in the query handler {HandlerName} Exception Message: {Message} Exception details: {StackTrace}.",
                    nameof(DmsArtifactUsageReferenceQueryService), e.Message, e.StackTrace);
                throw;
            }
        }
    }
}