using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class ActivateUserAccountService : IActivateUserAccount
    {
        private readonly ILogger<ActivateUserAccountService> _logger;
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;

        public ActivateUserAccountService(
            ILogger<ActivateUserAccountService> logger,
            IRepository repository,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider)
        {
            _logger = logger;
            _repository = repository;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        }

        public async Task ActivateAccount(string clientId)
        {
            try
            {
                var praxisUserRepo = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisUser>("PraxisUsers");
                var builder = Builders<PraxisUser>.Filter;
                var filter = builder.Eq("ClientList.ClientId", clientId);

                var deactivateRole = $"{nameof(RoleNames.Deactivate_Account)}_{clientId}";

                var praxisUserList = praxisUserRepo.Find(filter).ToList();
                foreach(var praxisUser in praxisUserList)
                {
                    try
                    { 
                        var roles = praxisUser.Roles.ToList();
                        roles.Remove(deactivateRole);

                        await UpdatePersonTableData(roles.ToArray(), praxisUser.ItemId);
                        await UpdateUserTableData(roles.ToArray(), praxisUser.UserId);
                        await UpdatePraxisUserTableData(roles.ToArray(), praxisUser.ItemId);
                        _repository.Delete<UserRoleMap>(u => u.RoleName == deactivateRole && u.UserId == praxisUser.UserId);
                        _repository.Delete<DataDeleteWarningNotification>(d => d.ClientId == clientId);
                        _logger.LogInformation("{DeactivateRole} data has been successfully removed from {EntityName} entity for user: {UserId}.",
                            deactivateRole, nameof(UserRoleMap), praxisUser.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Exception occurred during removing {DeactivateRole} role to personId: {PersonId}. Exception message: {ErrorMessage}. Exception Details: {StackTrace}.",
                            deactivateRole, praxisUser.ItemId, ex.Message, ex.StackTrace);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("Exception occurred during removing {RoleName} role from all users for client: {ClientId}. Exception message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    nameof(RoleNames.Deactivate_Account), clientId, ex.Message, ex.StackTrace);
            }
        }

        private async Task UpdatePersonTableData(string[] roles, string personId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                        {
                            {"Roles", roles},
                            {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()}
                        };
                await _repository.UpdateAsync<Person>(p => p.ItemId == personId, updates);
                _logger.LogInformation("Roles have been successfully updated to {EntityName} entity with ItemId: {PersonId}.", nameof(Person), personId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during updating {EntityName} entity with roles: {RolesJson} for ItemId: {PersonId}. Exception message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    nameof(Person), JsonConvert.SerializeObject(roles), personId, ex.Message, ex.StackTrace);
            }
        }

        private async Task UpdateUserTableData(string[] roles, string userId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                        {
                            {"Roles", roles},
                            {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()}
                        };
                await _repository.UpdateAsync<User>(p => p.ItemId == userId, updates);
                _logger.LogInformation("Roles have been successfully updated to {User} entity with ItemId: {UserId}.", nameof(User), userId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during updating {User} entity with roles: {RolesJson} for ItemId: {UserId}. Exception message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    nameof(User), JsonConvert.SerializeObject(roles), userId, ex.Message, ex.StackTrace);
            }
        }

        private async Task UpdatePraxisUserTableData(string[] roles, string PraxisUserId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                        {
                            {"Roles", roles},
                            {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()}
                        };
                await _repository.UpdateAsync<PraxisUser>(p => p.ItemId == PraxisUserId, updates);
                _logger.LogInformation("Roles have been successfully updated to {PraxisUser} entity with ItemId: {PraxisUserId}.", nameof(PraxisUser), PraxisUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during updating {PraxisUser} entity with roles: {RolesJson} for ItemId: {PraxisUserId}. Exception message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    nameof(PraxisUser), JsonConvert.SerializeObject(roles), PraxisUserId, ex.Message, ex.StackTrace);
            }
        }
    }
}
