using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class SaveDataToArchivedRoleService : ISaveDataToArchivedRole
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ILogger<SaveDataToArchivedRoleService> _logger;

        public SaveDataToArchivedRoleService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ILogger<SaveDataToArchivedRoleService> logger)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _logger = logger;
        }

        public void InsertData(EntityBase entity)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            _logger.LogInformation("Going to take backup for RLS of {EntityType} entity with ItemId: {ItemId} for tenant: {TenantId}", entity.GetType().Name, entity.ItemId, securityContext.TenantId);
            try
            {
                var newArchive = new ArchivedRole
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreatedBy = securityContext.UserId, 
                    CreateDate = DateTime.UtcNow,
                    EntityName = entity.GetType().Name,
                    EntityItemId = entity.ItemId,
                    ArchivedRolesAllowedToRead = entity.RolesAllowedToRead,
                    ArchivedIdsAllowedToRead = entity.IdsAllowedToRead,
                    ArchivedRolesAllowedToWrite = entity.RolesAllowedToWrite,
                    ArchivedIdsAllowedToWrite = entity.IdsAllowedToWrite,
                    ArchivedRolesAllowedToUpdate = entity.RolesAllowedToUpdate,
                    ArchivedIdsAllowedToUpdate = entity.IdsAllowedToUpdate,
                    ArchivedRolesAllowedToDelete = entity.RolesAllowedToDelete,
                    ArchivedIdsAllowedToDelete = entity.IdsAllowedToDelete,
                    ActionType = "Delete"
                };
                _repository.Save(newArchive);
                _logger.LogInformation("{EntityName} entity data has been successfully inserted with ItemId: {ItemId} for tenantId: {TenantId}", nameof(ArchivedRole), newArchive.ItemId, securityContext.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during save {nameof(ArchivedRole)} data for tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }
        public void UpdateArchiveRole(EntityBase entity,List<string> RelatedProperties,string EntityItemId)
        {
          var archiveRole=  _repository.GetItem<ArchivedRole>(o => o.EntityItemId == EntityItemId);
            if (archiveRole != null)
            {
                var relatedEntity = new RelatedEntity()
                {
                    EntityName = entity.GetType().Name,
                    EntityItemId = entity.ItemId,
                    RelatedProperties = RelatedProperties
                };
                
                if (archiveRole.RelatedEntityList != null)
                {
                    
                    var relatedEntityList = archiveRole.RelatedEntityList;
                    relatedEntityList.Add(relatedEntity);
                    archiveRole.RelatedEntityList = relatedEntityList;
                }               
                else
                {
                    var relatedEntities = new List<RelatedEntity>() { relatedEntity };
                    archiveRole.RelatedEntityList = relatedEntities;

                }
                _repository.Update(r => r.ItemId == archiveRole.ItemId,archiveRole);
            }
        }



    }
}
