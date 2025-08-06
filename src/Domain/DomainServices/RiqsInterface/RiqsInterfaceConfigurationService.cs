using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceConfigurationService : IRiqsInterfaceConfigurationService
    {
        private readonly ILogger<RiqsInterfaceConfigurationService> _logger;
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IPraxisEquipmentService _praxisEquipmentService;

        public RiqsInterfaceConfigurationService(
         ILogger<RiqsInterfaceConfigurationService> logger,
         IRepository repository,
         IBlocksMongoDbDataContextProvider ecapRepository,
         ISecurityContextProvider securityContextProvider,
         IPraxisEquipmentService praxisEquipmentService)
        {
            _logger = logger;
            _repository = repository;
            _ecapRepository = ecapRepository;
            _securityContextProvider = securityContextProvider;
            _praxisEquipmentService = praxisEquipmentService;
        }

        public async Task UpsertRiqsInterfaceConfiguration(UpsertRiqsInterfaceConfigurationCommand command)
        {
            try
            {
                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("RiqsInterfaceConfigurations");

                var filter = Builders<BsonDocument>.Filter.Eq("Provider", command.Provider);

                var document = command.ToBsonDocument();

                var result = await collection.ReplaceOneAsync(
                    filter,
                    document,
                    new UpdateOptions { IsUpsert = true }
                );

                _logger.LogInformation("Upsert operation completed. Matched: {MatchedCount}, Modified: {ModifiedCount}, UpsertedId: {UpsertedId}",
                    result.MatchedCount, result.ModifiedCount, result.UpsertedId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UpsertRiqsInterfaceConfiguration: {Message} | StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

    }
}
