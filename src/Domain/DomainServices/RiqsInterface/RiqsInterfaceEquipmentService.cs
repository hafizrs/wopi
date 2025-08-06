using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.Security;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceEquipmentService : IRiqsInterfaceEquipmentService
    {
        private readonly ILogger<RiqsInterfaceEquipmentService> _logger;
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IPraxisEquipmentService _praxisEquipmentService;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IPraxisRoomService _praxisRoomService;

        public RiqsInterfaceEquipmentService(
         ILogger<RiqsInterfaceEquipmentService> logger,
         IRepository repository,
         IBlocksMongoDbDataContextProvider ecapRepository,
         ISecurityContextProvider securityContextProvider,
         IPraxisEquipmentService praxisEquipmentService,
         IMongoSecurityService mongoSecurityService,
         IPraxisRoomService praxisRoomService)
        {
            _logger = logger;
            _repository = repository;
            _ecapRepository = ecapRepository;
            _securityContextProvider = securityContextProvider;
            _praxisEquipmentService = praxisEquipmentService;
            _mongoSecurityService = mongoSecurityService;
            _praxisRoomService = praxisRoomService;
        }

        public async Task CreateEquimentFromRiqsInterfaceMigration(CreateEquimentFromRiqsInterfaceMigrationCommand command)
        {
            if (string.IsNullOrEmpty(command.MigrationSummaryId) || !command.EquipmentIds.Any()) return;

            try
            {
                var equipmentFilterPipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument
                    {
                        { "MigrationSummeryId", command.MigrationSummaryId },
                        { "_id", new BsonDocument("$in", new BsonArray(command.EquipmentIds)) }
                    })
                };

                var documents = await _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("TempEquipmentInterfacePaseDatas")
                    .Aggregate<BsonDocument>(equipmentFilterPipeline)
                    .ToListAsync();

                var equipmentsToSave = documents
                    .Select(doc => BsonSerializer.Deserialize<PraxisEquipment>(doc))
                    .Where(equipment => command.EquipmentIds.Contains(equipment.ItemId))
                    .ToList();

                if (equipmentsToSave != null && equipmentsToSave.Any())
                {
                    var prepareEquipmentsToSave = PrepareRolePermissionForEquipments(equipmentsToSave);
                    var prepareEquipmentLocationHistoriesToSave = PrepareEquipmentLocationHistories(equipmentsToSave);
                    var insertedEquipmentResponses = await BulkUpsertEquipmentsAsync(prepareEquipmentsToSave);
                    await GenerateQrFileForEquipmentAsync(insertedEquipmentResponses);
                    await UpsertEquipmentLocationHistoriesAsync(prepareEquipmentLocationHistoriesToSave);
                }

                await CreateEquimentMaintenances(command);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(CreateEquimentFromRiqsInterfaceMigration), ex.Message, ex.StackTrace);
            }
        }

        private async Task<List<PraxisEquipment>> BulkUpsertEquipmentsAsync(List<PraxisEquipment> equipmentsToSave)
        {
            var successfullyProcessedEquipments = new List<PraxisEquipment>(equipmentsToSave);

            try
            {
                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("PraxisEquipments");

                var bulkOperations = equipmentsToSave.Select(equipment =>
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", equipment.ItemId);
                    var update = equipment.ToBsonDocument();
                    return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                }).ToList();

                var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);

                _logger.LogInformation("Bulk upsert completed. Matched: {MatchedCount}, Modified: {ModifiedCount}, Inserted: {InsertedCount}",
                    bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, bulkWriteResult.Upserts.Count);
            }
            catch (MongoBulkWriteException<BsonDocument> bulkEx)
            {
                var failedIndexes = bulkEx.WriteErrors.Select(error => error.Index).ToHashSet();

                var failedEquipments = failedIndexes
                    .Where(index => index >= 0 && index < equipmentsToSave.Count)
                    .Select(index => equipmentsToSave[index])
                    .ToHashSet();

                successfullyProcessedEquipments = equipmentsToSave.Where(e => !failedEquipments.Contains(e)).ToList();

                _logger.LogError("Bulk upsert failed for {Count} documents. Failed ItemIds: {FailedItemIds}",
                    failedEquipments.Count, string.Join(", ", failedEquipments.Select(e => e.ItemId)));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in BulkUpsertEquipmentsAsync: {Message} | StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            }

            return successfullyProcessedEquipments;
        }

        private List<PraxisEquipment> PrepareRolePermissionForEquipments(List<PraxisEquipment> equipmentsToSave)
        {
            foreach (var equipment in equipmentsToSave)
            {
                var clientAdminAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, equipment.ClientId);
                var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, equipment.ClientId);
                var clientManagerAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, equipment.ClientId);

                // Ensure arrays are not null
                equipment.RolesAllowedToRead ??= Array.Empty<string>();
                equipment.RolesAllowedToUpdate ??= Array.Empty<string>();
                equipment.RolesAllowedToDelete ??= Array.Empty<string>();

                equipment.RolesAllowedToRead = equipment.RolesAllowedToRead
                    .Append(clientReadAccessRole)
                    .Append(clientAdminAccessRole)
                    .Append(clientManagerAccessRole)
                    .Append(RoleNames.PowerUser)
                    .Append(RoleNames.MpaGroup1)
                    .Append(RoleNames.MpaGroup2)
                    .ToArray();

                equipment.RolesAllowedToUpdate = equipment.RolesAllowedToUpdate
                    .Append(clientAdminAccessRole)
                    .ToArray();

                equipment.RolesAllowedToDelete = equipment.RolesAllowedToDelete
                    .Append(clientAdminAccessRole)
                    .ToArray();
            }

            return equipmentsToSave;
        }

        private async Task GenerateQrFileForEquipmentAsync(List<PraxisEquipment> insertedEquipments)
        {
            await Parallel.ForEachAsync(insertedEquipments, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (equipment, _) =>
            {
                if (await _repository.ExistsAsync<PraxisEquipment>(e => e.ItemId == equipment.ItemId))
                {
                    await _praxisEquipmentService.GenerateQrFileForEquipment(equipment);
                }
            });
        }

        private async Task UpsertEquipmentLocationHistoriesAsync(List<PraxisEquipmentLocationHistory> locationHistories)
        {
            try
            {
                if(locationHistories.Count> 0)
                {
                    var collection = _ecapRepository
                   .GetTenantDataContext()
                   .GetCollection<BsonDocument>("PraxisEquipmentLocationHistorys");

                    var bulkOperations = locationHistories.Select(equipment =>
                    {
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", equipment.ItemId);
                        var update = equipment.ToBsonDocument();
                        return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                    }).ToList();

                    var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);

                    _logger.LogInformation("Bulk upsert completed. Matched: {MatchedCount}, Modified: {ModifiedCount}, Inserted: {InsertedCount}",
                        bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, bulkWriteResult.Upserts.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UpsertEquipmentLocationHistoryAsync: {Message} | StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        private List<PraxisEquipmentLocationHistory> PrepareEquipmentLocationHistories(List<PraxisEquipment> equipmentsToSave)
        {
            var locationHistories = new List<PraxisEquipmentLocationHistory>();

            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var user = _repository.GetItem<PraxisUser>(u => u.UserId == userId);
            var authorizedRoles = new[] { RoleNames.Admin, RoleNames.AppUser };
            var authorizedIds = new[] { userId };

            var updatedBy = new PraxisPersonInfo
            {
                PraxisUserId = user.ItemId,
                DisplayName = user.DisplayName,
                Email = user.Email
            };

            foreach (var equipment in equipmentsToSave)
            {
                if (equipment?.RoomId == null) continue;

                var currentLocationInfo = new PraxisEquipmentLocationInfo();
                var lastLocationInfo = new PraxisEquipmentLocationInfo();
                var currentRoom = _repository.GetItem<PraxisRoom>(p => p.ItemId == equipment.RoomId);

                if (currentRoom?.ItemId != null)
                {
                    currentLocationInfo = new PraxisEquipmentLocationInfo
                    {
                        LocationId = currentRoom?.ItemId,
                        LocationName = currentRoom?.Name,
                        address = currentRoom?.Address
                    };
                }

                if (currentLocationInfo?.LocationId == null) continue;

                var existingData = _repository.GetItem<PraxisEquipmentLocationHistory>(p => p.EquipmentId == equipment.ItemId);

                if (existingData?.LocationChangeLog?.CurrentLocationInfo?.LocationId == currentLocationInfo?.LocationId) continue;

                if (existingData != null && existingData?.LocationChangeLog?.CurrentLocationInfo?.LocationId != currentLocationInfo?.LocationId)
                {
                    lastLocationInfo = existingData?.LocationChangeLog?.CurrentLocationInfo;
                }

                var changeLog = new LocationChangeLog
                {
                    ChangeDate = DateTime.UtcNow.ToLocalTime(),
                    UpdatedBy = updatedBy,
                    LastLocationInfo = lastLocationInfo.LocationId != null ? lastLocationInfo : null,
                    CurrentLocationInfo = currentLocationInfo,
                };

                var locationHistory = new PraxisEquipmentLocationHistory
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow.ToLocalTime(),
                    CreatedBy = userId,
                    LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                    LastUpdatedBy = userId,
                    Language = "en-US",
                    IdsAllowedToRead = authorizedIds,
                    IdsAllowedToUpdate = authorizedIds,
                    IdsAllowedToDelete = authorizedIds,
                    RolesAllowedToRead = authorizedRoles,
                    Tags = new string[] { "Is-Valid-PraxisEquipmentLocationHistory" },
                    TenantId = _securityContextProvider.GetSecurityContext().TenantId,
                    EquipmentId = equipment.ItemId,
                    LocationChangeLog = changeLog,
                    Remarks = currentRoom.Remarks
                };

                locationHistories.Add(locationHistory);
            }

            return locationHistories;
        }

        private List<PraxisEquipmentMaintenance> PrepareRolePermissionForEquipmentMaintenances(List<PraxisEquipmentMaintenance> equipmentsToSave)
        {
            foreach (var equipment in equipmentsToSave)
            {
                var clientAdminAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, equipment.ClientId);
                var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, equipment.ClientId);
                var clientManagerAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, equipment.ClientId);

                equipment.RolesAllowedToRead ??= Array.Empty<string>();
                equipment.RolesAllowedToUpdate ??= Array.Empty<string>();
                equipment.RolesAllowedToDelete ??= Array.Empty<string>();

                equipment.RolesAllowedToRead = equipment.RolesAllowedToRead
                    .Append(clientReadAccessRole)
                    .Append(clientAdminAccessRole)
                    .Append(clientManagerAccessRole)
                    .Append(RoleNames.PowerUser)
                    .Append(RoleNames.MpaGroup1)
                    .Append(RoleNames.MpaGroup2)
                    .ToArray();

                equipment.RolesAllowedToUpdate = equipment.RolesAllowedToUpdate
                    .Append(clientAdminAccessRole)
                    .ToArray();

                equipment.RolesAllowedToDelete = equipment.RolesAllowedToDelete
                    .Append(clientAdminAccessRole)
                    .ToArray();
            }

            return equipmentsToSave;
        }

        private async Task CreateEquimentMaintenances(CreateEquimentFromRiqsInterfaceMigrationCommand command)
        {
            try
            {
                var equipmentMaintenancesFilterPipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument
                    {
                        { "MigrationSummeryId", command.MigrationSummaryId },
                        { "PraxisEquipmentId", new BsonDocument("$in", new BsonArray(command.EquipmentIds)) }
                    })
                };

                var documents = await _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("TempEquipmentMaintenancesInterfacePastDatas")
                    .Aggregate<BsonDocument>(equipmentMaintenancesFilterPipeline)
                    .ToListAsync();

                var equipmentMaintenancesToSave = documents
                    .Select(doc => BsonSerializer.Deserialize<PraxisEquipmentMaintenance>(doc))
                    .Where(equipmentMaintenance => command.EquipmentIds.Contains(equipmentMaintenance.PraxisEquipmentId))
                    .ToList();

                if (equipmentMaintenancesToSave != null && equipmentMaintenancesToSave.Any())
                {
                    var prepareEquipmentMaintenancesToSave = PrepareRolePermissionForEquipmentMaintenances(equipmentMaintenancesToSave);
                    await BulkUpsertEquipmentMaintenancesAsync(prepareEquipmentMaintenancesToSave);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(CreateEquimentMaintenances), ex.Message, ex.StackTrace);
            }
        }

        private async Task<List<PraxisEquipmentMaintenance>> BulkUpsertEquipmentMaintenancesAsync(List<PraxisEquipmentMaintenance> equipmentMaintenancesToSave)
        {
            var successfullyProcessedEquipmentMaintenances = new List<PraxisEquipmentMaintenance>(equipmentMaintenancesToSave);

            try
            {
                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("PraxisEquipmentMaintenances");

                var bulkOperations = equipmentMaintenancesToSave.Select(equipmentMaintenance =>
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", equipmentMaintenance.ItemId);
                    var update = equipmentMaintenance.ToBsonDocument();
                    return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                }).ToList();

                var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);

                _logger.LogInformation("Bulk upsert equipment maintenances completed. Matched: {MatchedCount}, Modified: {ModifiedCount}, Inserted: {InsertedCount}",
                    bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, bulkWriteResult.Upserts.Count);
            }
            catch (MongoBulkWriteException<BsonDocument> bulkEx)
            {
                var failedIndexes = bulkEx.WriteErrors.Select(error => error.Index).ToHashSet();

                var failedEquipmentMaintenances = failedIndexes
                    .Where(index => index >= 0 && index < equipmentMaintenancesToSave.Count)
                    .Select(index => equipmentMaintenancesToSave[index])
                    .ToHashSet();

                successfullyProcessedEquipmentMaintenances = equipmentMaintenancesToSave.Where(e => !failedEquipmentMaintenances.Contains(e)).ToList();

                _logger.LogError("Bulk upsert equipment maintenances failed for {Count} documents. Failed ItemIds: {FailedItemIds}",
                    failedEquipmentMaintenances.Count, string.Join(", ", failedEquipmentMaintenances.Select(e => e.ItemId)));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in BulkUpsertEquipmentMaintenancesAsync. Error: {ErrorMessage}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            }

            return successfullyProcessedEquipmentMaintenances;
        }
    }
}
