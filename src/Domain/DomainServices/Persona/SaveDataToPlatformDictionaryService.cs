using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona
{
    public class SaveDataToPlatformDictionaryService : ISaveDataToPlatformDictionary
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ILogger<SaveDataToPlatformDictionaryService> _logger;

        public SaveDataToPlatformDictionaryService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ILogger<SaveDataToPlatformDictionaryService> logger
            )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _logger = logger;
        }

        public bool SaveOrganizationInfoWithPersonaRole(string roleName, string organizationId)
        {
            try
            {
                _logger.LogInformation("Going to save Organization Information with Persona Role Name: {RoleName} to {EntityName} entity.", roleName, nameof(PlatformDictionary));
                var securityContext = _securityContextProvider.GetSecurityContext();

                var isExist = _repository.ExistsAsync<PlatformDictionary>(p =>p.Name == "PersonaRole" && p.Key == roleName).Result;
                if (!isExist)
                {
                    var existingOrganization =_repository.GetItem<PraxisClient>(c => c.ItemId == organizationId);
                    if (existingOrganization != null)
                    {
                        var rolesToAllow = new[] { "appuser" };
                        var idsToAllow = new[] { securityContext.UserId };

                        var newPlatformDictionary = new PlatformDictionary
                        {
                            ItemId = Guid.NewGuid().ToString(),
                            CreateDate = DateTime.Now.ToLocalTime(),
                            CreatedBy = securityContext.UserId,
                            Language = "en-US",
                            LastUpdateDate = DateTime.Now.ToLocalTime(),
                            LastUpdatedBy = securityContext.UserId,
                            Tags = new[] { "Dictionary" },
                            TenantId = securityContext.TenantId,
                            RolesAllowedToRead = rolesToAllow,
                            IdsAllowedToRead = idsToAllow,
                            RolesAllowedToUpdate = rolesToAllow,
                            IdsAllowedToUpdate = idsToAllow,
                            IdsAllowedToDelete = idsToAllow,
                            Name = "PersonaRole",
                            Key = roleName,
                            Value = "{\"ClientId\":\"" + existingOrganization.ItemId + "\",\"ClientName\":\"" + existingOrganization.ClientName + "\"}"
                        };

                        _repository.Save(newPlatformDictionary);
                        _logger.LogInformation("Data has been successfully inserted to {EntityName} with Name: {Name} and Key: {Key}.", nameof(PlatformDictionary), newPlatformDictionary.Name, newPlatformDictionary.Key);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while inserting to {EntityName} with Key: {Key} and for OrganizationId: {OrganizationId}. Exception Message: {Message}. Exception details: {StackTrace}.", nameof(PlatformDictionary), roleName, organizationId, ex.Message, ex.StackTrace);
                return false;
            }
        }
    }
}
