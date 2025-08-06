using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteUserRelatedData : IDeleteUserRelatedData
    {
        private readonly IRepository _repository;
        private readonly ILogger<DeleteUserRelatedData> _logger;
        private readonly INotificationService _notificationProviderService;
        private readonly IUserCountMaintainService _userCountMaintainService;

        public DeleteUserRelatedData(
            IRepository repository,
            ILogger<DeleteUserRelatedData> logger,
            INotificationService notificationProviderService,
            IUserCountMaintainService userCountMaintainService
            )
        {
            _notificationProviderService = notificationProviderService;
            _repository = repository;
            _logger = logger;
            _userCountMaintainService = userCountMaintainService;
        }

        public async Task<(bool, string, string)> DeleteData(string userId)
        {
            _logger.LogInformation($"Going to delete {nameof(User)} data with ItemId: {userId} for client admin.");

            var userItemId = string.Empty;
            var personItemId = string.Empty;
            try
            {
                var existingUser = await _repository.GetItemAsync<User>(u => u.ItemId == userId && !u.IsMarkedToDelete);
                if (existingUser != null)
                {
                    userItemId = existingUser.ItemId;
                    await UpdateUserData(existingUser);

                    var existingConnection = await _repository.GetItemAsync<Connection>(c => c.ParentEntityID == existingUser.ItemId && c.Tags.Contains(TagName.PersonForUser) && !c.IsMarkedToDelete);
                    if (existingConnection != null)
                    {
                        var existingPerson = await _repository.GetItemAsync<Person>(p => p.ItemId == existingConnection.ChildEntityID && !p.IsMarkedToDelete);
                        if(existingPerson != null)
                        {
                            personItemId = existingPerson.ItemId;
                            await UpdatePersonData(existingPerson);
                            await UpdatePraxisUserData(existingPerson.ItemId);
                            await DeleteGroupAdminData(existingPerson.ItemId);
                            await UpdatePraxisUserDtoData(existingPerson.ItemId);
                            await UpdateUserCount(existingPerson.ItemId);
                            await UpdateTaskSummaryAssignMember(existingPerson.ItemId);
                        }
                        await UpdateConnectionData(existingConnection);
                    }

                    await UpdateUserProfileData(existingUser.ItemId);

                    var userRoleMaps = _repository.GetItems<UserRoleMap>(ur => ur.UserId == existingUser.ItemId && !ur.IsMarkedToDelete).ToList();
                    if(userRoleMaps.Any())
                    {
                        await UpdateUserRoleMapData(userRoleMaps, existingUser.ItemId);
                    }

                    await UpdatePlatformUserIdentifierData(existingUser.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(User)}  entity related all data for FormId:{userId}. Exception Message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                await SendNotificationOfUser(userId, false);
                return (false, string.Empty, string.Empty);
            }

            return (true, userItemId, personItemId);
        }
        private async Task UpdateUserCount(string itemId)
        {
            var praxisUser = _repository.GetItem<PraxisUser>(x => x.ItemId == itemId);
            if (praxisUser != null && praxisUser.ClientList != null)
            {
                var primaryClient = praxisUser.ClientList.FirstOrDefault(c => c.IsPrimaryDepartment);
                if (primaryClient != null)
                {
                    await _userCountMaintainService.InitiateUserCountUpdateProcessOnUserCreate(primaryClient.ClientId, primaryClient.ParentOrganizationId);
                }
            }
        }
        private async Task UpdateUserData(User user)
        {
            try
            {
                user.IsMarkedToDelete = true;
                user.DisplayName = "xxxxxxxxx xxxxxxxx";
                user.Email = "xxxxx@xxx.com";
                user.UserName = "xxxxx@xxx.com";
                user.FirstName = "xxxxxxx";
                user.LastName = "xxxxxxx";

                var updates = new Dictionary<string, object>
                    {
                        {"DisplayName", user.DisplayName},
                        {"Email", user.Email},
                        {"UserName", user.UserName},
                        {"FirstName", user.FirstName},
                        {"LastName", user.LastName},
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", user.IsMarkedToDelete}
                    };

                await _repository.UpdateAsync<User>(c => c.ItemId == user.ItemId, updates);
                _logger.LogInformation($"Data has been successfully updated for {nameof(User)} entity with ItemId: {user.ItemId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(User)} data with ItemId: {user.ItemId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        private async Task UpdatePersonData(Person person)
        {
            try
            {
                person.IsMarkedToDelete = true;
                person.DisplayName = "xxxxxxxxx xxxxxxxx";
                person.Email = "xxxxx@xxx.com";
                person.FirstName = "xxxxxxx";
                person.LastName = "xxxxxxx";
                person.PhoneNumber = "xxxxxxx";
                var updates = new Dictionary<string, object>
                    {
                        {"DisplayName", person.DisplayName},
                        {"Email", person.Email},
                        {"FirstName", person.FirstName},
                        {"LastName", person.LastName},
                        {"PhoneNumber", person.PhoneNumber},
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", person.IsMarkedToDelete},
                    };

                await _repository.UpdateAsync<Person>(c => c.ItemId == person.ItemId, updates);
                _logger.LogInformation($"Data has been successfully updated for {nameof(Person)} entity with ItemId: {person.ItemId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(Person)} data with ItemId: {person.ItemId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        private async Task UpdatePraxisUserData(string itemId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                    {
                        {"DisplayName", "xxxxxxxxx xxxxxxxx"},
                        {"Email", "xxxxx@xxx.com"},
                        {"FirstName", "xxxxxxx"},
                        {"LastName", "xxxxxxx"},
                        {"Phone", "xxxxxxxxxx"},
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", true},
                    };

                await _repository.UpdateAsync<PraxisUser>(c => c.ItemId == itemId, updates);
                _logger.LogInformation($"Data has been successfully updated for {nameof(PraxisUser)} entity with ItemId: {itemId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(PraxisUser)} data with ItemId: {itemId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        private async Task DeleteGroupAdminData(string praxisUserId)
        {
            try
            {
                await _repository.DeleteAsync<RiqsGroupAdmin>(c => c.PraxisUserId == praxisUserId);
                _logger.LogInformation($"Data has been successfully deleted for {nameof(RiqsGroupAdmin)} entity with PraxisUserId: {praxisUserId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during DeleteGroupAdminData data with praxisUserId: {praxisUserId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        private async Task UpdatePraxisUserDtoData(string praxisUserId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                    {
                        {"DisplayName", "xxxxxxxxx xxxxxxxx"},
                        {"IsActive", false},
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", true},
                    };

                await _repository.UpdateAsync<PraxisUserDto>(c => c.PraxisUserId == praxisUserId, updates);
                _logger.LogInformation($"Data has been successfully updated for {nameof(PraxisUserDto)} entity with PraxisUserId: {praxisUserId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(PraxisUser)} data with ItemId: {praxisUserId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }
        private async Task UpdateUserProfileData(string userId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", true},
                    };

                await _repository.UpdateAsync<UserProfile>(c => c.UserId == userId, updates);
                _logger.LogInformation($"Data has been successfully updated for {nameof(UserProfile)} entity with userId: {userId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(UserProfile)} data with userId: {userId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        private async Task UpdateUserRoleMapData(List<UserRoleMap> userRoleMaps, string userId)
        {
            try
            {
                foreach(var userRoleMap in userRoleMaps)
                {
                    userRoleMap.IsMarkedToDelete = true;

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", userRoleMap.IsMarkedToDelete},
                    };

                    await _repository.UpdateAsync<UserRoleMap>(c => c.ItemId == userRoleMap.ItemId, updates);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(UserRoleMap)} data with UserId: {userId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        private async Task UpdatePlatformUserIdentifierData(string userId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", true},
                    };

                await _repository.UpdateAsync<PlatformUserIdentifier>(c => c.EcapUserId == userId, updates);
                _logger.LogInformation($"Data has been successfully updated for {nameof(PlatformUserIdentifier)} entity with userId: {userId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(PlatformUserIdentifier)} data with userId: {userId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        private async Task UpdateTaskSummaryAssignMember(string personId)
        {
            try
            {
                var taskSummaryList = _repository.GetItems<TaskSummary>(x => x.AssignMembers.Any(asm => asm.PersonId == personId)).ToList();
                foreach(var taskSummary in taskSummaryList)
                {
                    taskSummary.AssignMembers = taskSummary.AssignMembers.Where(x => x.PersonId != personId).ToList();
                    await _repository.UpdateAsync(x => x.ItemId == taskSummary.ItemId, taskSummary);
                }
                _logger.LogInformation($"remove assigned member for personId -> {personId}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Exception on remove assigned member with personId -> {personId} and Error -> {ex.Message}");
            }
        }

        private async Task UpdateConnectionData(Connection connection)
        {
            try
            {
                connection.IsMarkedToDelete = true;

                var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", connection.IsMarkedToDelete},
                    };

                await _repository.UpdateAsync<Connection>(c => c.ItemId == connection.ItemId, updates);
                _logger.LogInformation($"Data has been successfully updated for {nameof(Connection)} entity with ItemId: {connection.ItemId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(Connection)} data with ItemId: {connection.ItemId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        public async Task SendNotificationOfUser(string userId, bool success)
        {
            var result = new
            {
                NotifiySubscriptionId = userId,
                Success = success,
                userId
            };
            await _notificationProviderService.DataDeleteNotifyToClient(success, userId, result, null, "Delete", "DeleteUser");
        }
    }
}
