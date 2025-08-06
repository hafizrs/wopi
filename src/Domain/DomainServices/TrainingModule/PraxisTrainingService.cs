using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisTrainingService : IPraxisTrainingService, IDeleteDataForClientInCollections
    {
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly ILogger<PraxisTrainingService> _logger;

        public PraxisTrainingService(
            IMongoSecurityService mongoSecurityService,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            ILogger<PraxisTrainingService> logger
        )
        {
            _mongoSecurityService = mongoSecurityService;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _logger = logger;
        }

        public void AddRowLevelSecurity(string itemId, string clientId)
        {
            var clientAdminAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
            var clientManagerAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);

            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);
            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);

            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisTraining>(permission);
        }

        public async Task<EntityQueryResponse<PraxisTraining>> GetTrainingReportData(string filter, string sort)
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

                var collections = _mongoDbDataContextProvider
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>($"PraxisTrainings")
                    .Aggregate()
                    .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();

                if (!string.IsNullOrEmpty(sort))
                {
                    collections = collections.Sort(BsonDocument.Parse(sort));
                }

                var results = collections.ToEnumerable()
                    .Select(document => BsonSerializer.Deserialize<PraxisTraining>(document));

                return new EntityQueryResponse<PraxisTraining>
                {
                    Results = results.ToList(),
                    TotalRecordCount = totalRecord
                };
            });
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation("Going to delete {PraxisTraining} for client {ClientId}", nameof(PraxisTraining), clientId);
            try
            {
                await _repository.DeleteAsync<PraxisTraining>(training => training.ClientId.Equals(clientId));
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {PraxisTraining} for client {ClientId}. Error: {Message}. Stacktrace: {StackTrace}.", nameof(PraxisTraining), clientId, e.Message, e.StackTrace);
            }
        }

    }
}
