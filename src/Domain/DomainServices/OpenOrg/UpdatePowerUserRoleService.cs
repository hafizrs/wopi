using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.OpenOrg
{
    public class UpdatePowerUserRoleService : IUpdatePowerUserRole
    {
        private readonly ILogger<UpdatePowerUserRoleService> _logger;
        private readonly IRepository _repository;

        public UpdatePowerUserRoleService(
            ILogger<UpdatePowerUserRoleService> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }


        public async Task<bool> UpdateRole(List<PraxisUser> userList, string updateRole, string removeRole)
        {
            _logger.LogInformation("Going to update all power user role with role: {UpdateRole}.", updateRole);

            foreach (var praxisUser in userList)
            {
                try
                {
                    var existingRoles = praxisUser.Roles.ToList();
                    existingRoles.Remove(removeRole);

                    if (!existingRoles.Contains(updateRole))
                    {
                        existingRoles.Add(updateRole);
                    }
                    
                    var updates = new Dictionary<string, object>
                    {
                        {"Roles", existingRoles },
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime() }
                    };

                    await _repository.UpdateAsync<PraxisUser>(pu => pu.ItemId == praxisUser.ItemId, updates);
                    _logger.LogInformation("Roles has been successfully updated to {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisUser), praxisUser.ItemId);

                    await _repository.UpdateAsync<Person>(p => p.ItemId == praxisUser.ItemId, updates);
                    _logger.LogInformation("Roles has been successfully updated to {EntityName} entity with ItemId: {ItemId}.", nameof(Person), praxisUser.ItemId);

                    await _repository.UpdateAsync<User>(u => u.ItemId == praxisUser.UserId, updates);
                    _logger.LogInformation("Roles has been successfully updated to {EntityName} entity with ItemId: {ItemId}.", nameof(User), praxisUser.UserId);

                    var userRoleMap = await _repository.GetItemAsync<UserRoleMap>(r => r.UserId == praxisUser.UserId && r.RoleName == removeRole);
                    if (userRoleMap != null)
                    {
                        userRoleMap.RoleName = updateRole;

                        await _repository.UpdateAsync<UserRoleMap>(u => u.ItemId == userRoleMap.ItemId, userRoleMap);
                        _logger.LogInformation("Roles has been successfully updated to {EntityName} entity with ItemId: {ItemId}.", nameof(UserRoleMap), userRoleMap.ItemId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception occured during update role for Person Id: {PersonId}. Exception Message: {Message}. Exception Details: {StackTrace}.", praxisUser.ItemId, ex.Message, ex.StackTrace);
                    return false;
                }
            }

            return true;
        }
    }
}
