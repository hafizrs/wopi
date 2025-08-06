using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Helpers;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.MongoDb
{
    public class MongoSecurityService : IMongoSecurityService
    {
        private readonly IRepository _repository;
        private readonly IMongoClientRepository _mongoClientRepository;
        private readonly ILogger<MongoSecurityService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;

        public MongoSecurityService(
            IRepository repository,
            IMongoClientRepository mongoClientRepository,
            ILogger<MongoSecurityService> logger,
            ISecurityContextProvider securityContextProvider
        )
        {
            _repository = repository;
            _logger = logger;
            _mongoClientRepository = mongoClientRepository;
            _securityContextProvider = securityContextProvider;
        }

        public User GetUserById(Guid userId)
        {
            try
            {
                return _repository.GetItem<User>(u => u.ItemId.Equals(userId.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogInformation("{LogMessageException} GetUserById :: userId -> {UserId}", LogHelpers.LogMessageException, userId);
            }

            return null;
        }

        public Person GetPersonByUserId(Guid userId)
        {
            try
            {
                return _repository.GetItem<Person>(u => u.CreatedBy.Equals(userId.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} GetPersonByUserId :: userId -> {UserId}", LogHelpers.LogMessageException, userId);
            }

            return null;
        }

        public Person GetPersonById(Guid personId)
        {
            try
            {
                return _repository.GetItem<Person>(u => u.ItemId.Equals(personId.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} GetPersonById :: personId -> {PersonId}", LogHelpers.LogMessageException, personId);
            }

            return null;
        }

        public string GetRoleName(string roleSlug, string itemId)
        {
            return $"{roleSlug}_{itemId}";
        }

        public bool IsRoleExist(string roleName)
        {
            try
            {
                var roleInfo = _repository.GetItem<Role>(u => u.RoleName.Equals(roleName));
                return roleInfo != null;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} IsRoleExist :: roleName -> {RoleName}", LogHelpers.LogMessageException, roleName);
            }

            return true;
        }

        public string CreateRole(string roleName, bool isDynamic)
        {
            try
            {
                if (IsRoleExist(roleName))
                {
                    return roleName;
                }

                var roles = new[] { "admin", "appuser" };

                var createdBy = string.Empty;
                var authData = _securityContextProvider.GetSecurityContext();
                if (!string.IsNullOrEmpty(authData.UserId))
                {
                    createdBy = authData.UserId.ToString();
                }

                var role = new Role
                {
                    Tags = null,
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow,
                    LastUpdateDate = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    LastUpdatedBy = createdBy,
                    IsDynamic = true,
                    RoleName = roleName,
                    RolesAllowedToRead = roles,
                    RolesAllowedToUpdate = roles,
                    RolesAllowedToDelete = new[] { "admin" }
                };
                _repository.Save(role);

                _logger.LogInformation("{LogMessageSuccess} CreateRole :: roleName -> {RoleName}, isDynamic -> {IsDynamic}, {Payload}",
                    LogHelpers.LogMessageSuccess, roleName, isDynamic, LogHelpers.JsonToString("payload", role));
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} CreateRole :: roleName -> {RoleName}, isDynamic -> {IsDynamic}",
                    LogHelpers.LogMessageException, roleName, isDynamic);
            }

            return roleName;
        }

        public EntityAccessPermission GetRowLevelSecurity<T>(Guid entityId)
        {
            string entityName = typeof(T).Name;
            try
            {
                var details = _mongoClientRepository.GetEntityDetials<T>(entityId.ToString());
                if (details == null)
                {
                    _logger.LogInformation("{LogMessageFail} GetRowLevelSecurity :: entityName -> {EntityName}, entityId -> {EntityId}",
                        LogHelpers.LogMessageFail, entityName, entityId);
                }
                else
                {
                    var entityAccessPermission = new EntityAccessPermission
                    {
                        RolesAllowedToRead = details.RolesAllowedToRead?.ToList(),
                        RolesAllowedToUpdate = details.RolesAllowedToUpdate?.ToList(),
                        RolesAllowedToDelete = details.RolesAllowedToDelete?.ToList(),
                        IdsAllowedToRead = details.IdsAllowedToRead?.ToList(),
                        IdsAllowedToUpdate = details.IdsAllowedToUpdate?.ToList(),
                        IdsAllowedToDelete = details.IdsAllowedToDelete?.ToList()
                    };

                    return entityAccessPermission;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} GetRowLevelSecurity :: entityName -> {EntityName}, entityId -> {EntityId}",
                    LogHelpers.LogMessageException, entityName, entityId);
            }

            return null;
        }

        public void UpdateEntityReadWritePermission<T>(EntityReadWritePermission permission)
        {
            _logger.LogInformation("Entered UpdateEntityReadWritePermission of MongoSecurity Service with permission: {Permission}", permission);
            var entityName = typeof(T).Name;
            try
            {
                if (!string.IsNullOrEmpty(entityName) && permission.Id != Guid.Empty)
                {
                    UpdateEntityReadWritePermission<T>(permission.Id, permission);
                }
                else
                {
                    _logger.LogInformation("{LogMessageFail} UpdateEntityReadWritePermission :: entityName -> {EntityName}, entityId -> {EntityId}",
                        LogHelpers.LogMessageFail, entityName, permission?.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} UpdateEntityReadWritePermission :: entityName -> {EntityName}, entityId -> {EntityId}",
                    LogHelpers.LogMessageException, entityName, permission?.Id);
            }
        }

        public void UpdateEntityReadWritePermission<T>(Guid entityId, EntityReadWritePermission permission)
        {
            _logger.LogInformation("Entered UpdateEntityReadWritePermission of MongoSecurityService");
            string entityName = typeof(T).Name;
            try
            {
                var accessPermission = GetRowLevelSecurity<T>(entityId);

                _logger.LogInformation("RowLevelSecurity of {EntityName} with ItemId {EntityId}", entityName, entityId);

                if (accessPermission == null) return;

                // assing permission
                if (permission.RolesAllowedToReadForRemove != null && permission.RolesAllowedToReadForRemove.Count > 0)
                {
                    accessPermission.RolesAllowedToRead?.RemoveAll(permission.RolesAllowedToReadForRemove.Contains);
                }

                if (permission.RolesAllowedToUpdateForRemove != null &&
                    permission.RolesAllowedToUpdateForRemove.Count > 0)
                {
                    accessPermission.RolesAllowedToUpdate?.RemoveAll(permission.RolesAllowedToUpdateForRemove.Contains);
                }

                if (permission.RolesAllowedToDeleteForRemove != null &&
                    permission.RolesAllowedToDeleteForRemove.Count > 0)
                {
                    accessPermission.RolesAllowedToDelete?.RemoveAll(permission.RolesAllowedToDeleteForRemove.Contains);
                }

                if (permission.RolesAllowedToRead != null && permission.RolesAllowedToRead.Count > 0)
                {
                    accessPermission.RolesAllowedToRead?.AddRange(permission.RolesAllowedToRead);
                }

                if (permission.RolesAllowedToUpdate != null && permission.RolesAllowedToUpdate.Count > 0)
                {
                    accessPermission.RolesAllowedToUpdate?.AddRange(permission.RolesAllowedToUpdate);
                }

                if (permission.RolesAllowedToDelete != null && permission.RolesAllowedToDelete.Count > 0)
                {
                    accessPermission.RolesAllowedToDelete?.AddRange(permission.RolesAllowedToDelete);
                }

                if (permission.IdsAllowedToRead != null && permission.IdsAllowedToRead.Count > 0)
                {
                    accessPermission.IdsAllowedToRead?.AddRange(permission.IdsAllowedToRead);
                }

                if (permission.IdsAllowedToReadForRemove != null && permission.IdsAllowedToReadForRemove.Count > 0)
                {
                    accessPermission.IdsAllowedToRead?.RemoveAll(permission.IdsAllowedToReadForRemove.Contains);
                }

                if (permission.IdsAllowedToUpdate != null && permission.IdsAllowedToUpdate.Count > 0)
                {
                    accessPermission.IdsAllowedToUpdate?.AddRange(permission.IdsAllowedToUpdate);
                }

                if (permission.IdsAllowedToUpdateForRemove != null && permission.IdsAllowedToUpdateForRemove.Count > 0)
                {
                    accessPermission.IdsAllowedToUpdate?.RemoveAll(permission.IdsAllowedToUpdateForRemove.Contains);
                }

                // Update Entity RowLevelSecurity
                var details = _mongoClientRepository.GetEntityDetials<T>(entityId.ToString());

                _logger.LogInformation("Entity details of {EntityName} with ItemId {EntityId} is {Details}", entityName, entityId, details);

                if (details == null) return;

                var rolesAllowedToRead = accessPermission.RolesAllowedToRead?.Distinct().ToArray();
                var rolesAllowedToUpdate = accessPermission.RolesAllowedToUpdate?.Distinct().ToArray();
                var rolesAllowedToDelete = accessPermission.RolesAllowedToDelete?.Distinct().ToArray();
                var idsAllowedToRead = accessPermission.IdsAllowedToRead?.Distinct().ToArray();
                var idsAllowedToUpdate = accessPermission.IdsAllowedToUpdate?.Distinct().ToArray();
                var idsAllowedToDelete = accessPermission.IdsAllowedToDelete?.Distinct().ToArray();


                var updates = new Dictionary<string, object>
                {
                    { "RolesAllowedToRead", rolesAllowedToRead },
                    { "RolesAllowedToUpdate", rolesAllowedToUpdate },
                    { "RolesAllowedToDelete", rolesAllowedToDelete },
                    { "IdsAllowedToRead", idsAllowedToRead },
                    { "IdsAllowedToUpdate", idsAllowedToUpdate },
                    { "IdsAllowedToDelete", idsAllowedToDelete }
                };

                _logger.LogInformation("Updating {EntityName} with Item Id {EntityId} and RolesAllowedToRead is {RolesAllowedToRead}", entityName, entityId, rolesAllowedToRead.Last());

                _mongoClientRepository.Update(entityName, entityId.ToString(), updates);

                _logger.LogInformation("Updated {EntityName} with Item Id {EntityId} and RolesAllowedToRead is {RolesAllowedToRead}", entityName, entityId, rolesAllowedToRead.Last());

            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateEntityReadWritePermission :: entityName -> {EntityName}, entityId -> {EntityId} -> {ErrorMessage} -> {StackTrace}",
                    entityName, permission?.Id, ex.Message, ex.StackTrace);
            }
        }

        public void UpdateRowLevelSecurityOfConnection(string connectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId)) return;

                var permission = GetUpdatedRowLevelSecurityPayloadOfConnection(connectionId);

                if (permission == null) return;

                UpdateEntityReadWritePermission<Connection>(Guid.Parse(connectionId), permission);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} UpdateRowLevelSecurityOfConnection :: connectionId -> {ConnectionId}",
                    LogHelpers.LogMessageException, connectionId);
            }
        }

        public void UpdateRowLevelSecurityOfConnection(
            string connectionId,
            EntityReadWritePermission entityReadWritePermission
        )
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId)) return;

                var permission = GetUpdatedRowLevelSecurityPayloadOfConnection(connectionId, entityReadWritePermission);

                if (permission == null) return;

                UpdateEntityReadWritePermission<Connection>(Guid.Parse(connectionId), permission);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} UpdateRowLevelSecurityOfConnection :: connectionId -> {ConnectionId}",
                    LogHelpers.LogMessageException, connectionId);
            }
        }

        public EntityReadWritePermission GetUpdatedRowLevelSecurityPayloadOfConnection(string connectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId)) return null;

                var connectionDetails = _mongoClientRepository.GetEntityDetials<Connection>(connectionId);

                if (connectionDetails is Connection details)
                {
                    var parentDetails = _mongoClientRepository.GetEntityDetials<Connection>(details.ParentEntityID);
                    var childDetails = _mongoClientRepository.GetEntityDetials<Connection>(details.ChildEntityID);

                    if (parentDetails != null && childDetails != null)
                    {
                        var permission = new EntityReadWritePermission();

                        // Child Details Role Data Update
                        if (childDetails.IdsAllowedToDelete != null)
                        {
                            permission.IdsAllowedToDelete.AddRange(childDetails.IdsAllowedToDelete);
                        }

                        if (childDetails.RolesAllowedToDelete != null)
                        {
                            permission.RolesAllowedToDelete.AddRange(childDetails.RolesAllowedToDelete);
                        }

                        if (childDetails.IdsAllowedToUpdate != null)
                        {
                            permission.IdsAllowedToUpdate.AddRange(childDetails.IdsAllowedToUpdate);
                        }

                        if (childDetails.RolesAllowedToUpdate != null)
                        {
                            permission.RolesAllowedToUpdate.AddRange(childDetails.RolesAllowedToUpdate);
                        }

                        if (childDetails.IdsAllowedToRead != null)
                        {
                            permission.IdsAllowedToRead.AddRange(childDetails.IdsAllowedToRead);
                        }

                        if (childDetails.RolesAllowedToRead != null)
                        {
                            permission.RolesAllowedToRead.AddRange(childDetails.RolesAllowedToRead);
                        }

                        // Parent Details Role Data Update
                        if (parentDetails.IdsAllowedToDelete != null)
                        {
                            permission.IdsAllowedToDelete.AddRange(parentDetails.IdsAllowedToDelete);
                        }

                        if (parentDetails.RolesAllowedToDelete != null)
                        {
                            permission.RolesAllowedToDelete.AddRange(parentDetails.RolesAllowedToDelete);
                        }

                        if (parentDetails.IdsAllowedToUpdate != null)
                        {
                            permission.IdsAllowedToUpdate.AddRange(parentDetails.IdsAllowedToUpdate);
                        }

                        if (parentDetails.RolesAllowedToUpdate != null)
                        {
                            permission.RolesAllowedToUpdate.AddRange(parentDetails.RolesAllowedToUpdate);
                        }

                        if (parentDetails.IdsAllowedToRead != null)
                        {
                            permission.IdsAllowedToRead.AddRange(parentDetails.IdsAllowedToRead);
                        }

                        if (parentDetails.RolesAllowedToRead != null)
                        {
                            permission.RolesAllowedToRead.AddRange(parentDetails.RolesAllowedToRead);
                        }

                        return permission;
                    }
                    else
                    {
                        _logger.LogInformation("{LogMessageFail} GetUpdatedRowLevelSecurityPayloadOfConnection Parent or Child got null. Parent EntityName {ParentEntityName} and ID {ParentEntityID}, Child EntityName {ChildEntityName} and ID {ChildEntityID}",
                            LogHelpers.LogMessageFail, details.ParentEntityName, details.ParentEntityID, details.ChildEntityName, details.ChildEntityID);
                    }
                }
                else
                {
                    _logger.LogInformation("{LogMessageFail} GetUpdatedRowLevelSecurityPayloadOfConnection :: ConnectionDetails got null. connectionId -> {ConnectionId}",
                        LogHelpers.LogMessageFail, connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} GetUpdatedRowLevelSecurityPayloadOfConnection :: connectionId -> {ConnectionId}",
                    LogHelpers.LogMessageException, connectionId);
            }

            return null;
        }

        public EntityReadWritePermission GetUpdatedRowLevelSecurityPayloadOfConnection(
            string connectionId,
            EntityReadWritePermission entityReadWritePermission
        )
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId)) return null;

                var permission = GetUpdatedRowLevelSecurityPayloadOfConnection(connectionId);

                if (permission != null)
                {
                    // Add Role form Extarnal EntityReadWritePermission
                    if (entityReadWritePermission.IdsAllowedToDelete != null)
                    {
                        permission.IdsAllowedToDelete.AddRange(entityReadWritePermission.IdsAllowedToDelete);
                    }

                    if (entityReadWritePermission.RolesAllowedToDelete != null)
                    {
                        permission.RolesAllowedToDelete.AddRange(entityReadWritePermission.RolesAllowedToDelete);
                    }

                    if (entityReadWritePermission.IdsAllowedToUpdate != null)
                    {
                        permission.IdsAllowedToUpdate.AddRange(entityReadWritePermission.IdsAllowedToUpdate);
                    }

                    if (entityReadWritePermission.RolesAllowedToUpdate != null)
                    {
                        permission.RolesAllowedToUpdate.AddRange(entityReadWritePermission.RolesAllowedToUpdate);
                    }

                    if (entityReadWritePermission.IdsAllowedToRead != null)
                    {
                        permission.IdsAllowedToRead.AddRange(entityReadWritePermission.IdsAllowedToRead);
                    }

                    if (entityReadWritePermission.RolesAllowedToRead != null)
                    {
                        permission.RolesAllowedToRead.AddRange(entityReadWritePermission.RolesAllowedToRead);
                    }

                    // Remove Role form Extarnal EntityReadWritePermission
                    if (entityReadWritePermission.IdsAllowedToDeleteForRemove != null)
                    {
                        permission.IdsAllowedToDeleteForRemove.AddRange(
                            entityReadWritePermission.IdsAllowedToDeleteForRemove
                        );
                    }

                    if (entityReadWritePermission.RolesAllowedToDeleteForRemove != null)
                    {
                        permission.RolesAllowedToDeleteForRemove.AddRange(
                            entityReadWritePermission.RolesAllowedToDeleteForRemove
                        );
                    }

                    if (entityReadWritePermission.IdsAllowedToUpdateForRemove != null)
                    {
                        permission.IdsAllowedToUpdateForRemove.AddRange(
                            entityReadWritePermission.IdsAllowedToUpdateForRemove
                        );
                    }

                    if (entityReadWritePermission.RolesAllowedToUpdateForRemove != null)
                    {
                        permission.RolesAllowedToUpdateForRemove.AddRange(
                            entityReadWritePermission.RolesAllowedToUpdateForRemove
                        );
                    }

                    if (entityReadWritePermission.IdsAllowedToReadForRemove != null)
                    {
                        permission.IdsAllowedToReadForRemove.AddRange(
                            entityReadWritePermission.IdsAllowedToReadForRemove
                        );
                    }

                    if (entityReadWritePermission.RolesAllowedToReadForRemove != null)
                    {
                        permission.RolesAllowedToReadForRemove.AddRange(
                            entityReadWritePermission.RolesAllowedToReadForRemove
                        );
                    }

                    return permission;
                }
                else
                {
                    _logger.LogInformation("{LogMessageFail} GetUpdatedRowLevelSecurityPayloadOfConnection :: permission -> null, entityReadWritePermission -> {EntityReadWritePermission}",
                        LogHelpers.LogMessageFail, entityReadWritePermission);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} GetUpdatedRowLevelSecurityPayloadOfConnection :: connectionId -> {ConnectionId}, entityReadWritePermission -> {EntityReadWritePermission}",
                    LogHelpers.LogMessageException, connectionId, entityReadWritePermission);
            }

            return null;
        }

        public void AssignRoleToUser(Guid userId, List<string> roles, bool onlyActiveUser = true)
        {
            try
            {
                var user = _repository.GetItem<User>(u => u.ItemId.Equals(userId.ToString()));
                if (user != null)
                {
                    if (onlyActiveUser && !user.Active)
                    {
                        _logger.LogInformation(
                            "{LogMessageFail}User Not Activated, Active Requierd For UserAssign",
                            LogHelpers.LogMessageFail
                        );
                        return;
                    }

                    var person = _repository.GetItem<Person>(u => u.CreatedBy.Equals(userId.ToString()));

                    var hasUpdate = false;
                    var userRoles = new List<string>();
                    if (user.Roles != null && user.Roles.Length > 0)
                    {
                        userRoles.AddRange(user.Roles.ToList());
                    }

                    foreach (var role in roles)
                    {
                        var hasRole = _repository.GetItem<Role>(urm => urm.RoleName.Equals(role));

                        if (!string.IsNullOrEmpty(hasRole?.RoleName))
                        {
                            var userRoleMap = new UserRoleMap
                            {
                                ItemId = Guid.NewGuid().ToString(),
                                TenantId = user.TenantId,
                                RoleName = role,
                                UserName = user.UserName,
                                UserId = user.ItemId
                            };

                            var roleMap = _repository.GetItem<UserRoleMap>(
                                urm => urm.UserId.Equals(userId.ToString()) && urm.RoleName.Equals(role)
                            );
                            if (string.IsNullOrEmpty(roleMap?.RoleName))
                            {
                                hasUpdate = true;
                                _repository.Save(userRoleMap);

                                _logger.LogInformation("{LogMessageSuccess} AssignRoleToUser (Add in UserRoleMap) :: userId -> {UserId}, role -> {Role}, {UserRoleMap}",
                                    LogHelpers.LogMessageSuccess, userId, role, LogHelpers.JsonToString("userRoleMap", userRoleMap));

                                userRoles.Add(role);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("{LogMessageFail} AssignRoleToUser (Role Not Found) role -> {Role}",
                                LogHelpers.LogMessageFail, role);
                        }
                    }

                    if (!hasUpdate) return;

                    var userUpdate = new Dictionary<string, object>
                    {
                        { "Roles", userRoles.Distinct().ToArray() }
                    };
                    var personUpdate = new Dictionary<string, object>
                    {
                        { "Roles", userRoles.Distinct().ToArray() }
                    };

                    _mongoClientRepository.Update(EntityNames.User, user.ItemId, userUpdate);
                    _logger.LogInformation("{LogMessageSuccess} AssignRoleToUser :: userId -> {UserId}",
                        LogHelpers.LogMessageSuccess, user.ItemId);


                    _mongoClientRepository.Update(EntityNames.Person, person.ItemId, personUpdate);
                    _logger.LogInformation("{LogMessageSuccess} AssignRoleToUser for Person :: personId -> {PersonId}",
                        LogHelpers.LogMessageSuccess, person.ItemId);
                }
                else
                {
                    _logger.LogInformation("{LogMessageFail} User Get Null, AssignRoleToUser :: userId -> {UserId}, {Roles}",
                        LogHelpers.LogMessageFail, userId, LogHelpers.JsonToString("roles", roles));
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} UnassignRoleFromUser :: userId -> {UserId}, roles -> {Roles}",
                    LogHelpers.LogMessageException, userId, roles);
            }
        }

        public void UnassignRoleFromUser(Guid userId, List<string> roles, bool onlyActiveUser = true)
        {
            try
            {
                var user = _repository.GetItem<User>(u => u.ItemId.Equals(userId.ToString()));
                if (user != null)
                {
                    if (onlyActiveUser && !user.Active)
                    {
                        _logger.LogInformation("{LogMessageFail} User Not Activated, Active Required For UserAssign",
                            LogHelpers.LogMessageFail);
                        return;
                    }

                    var person = _repository.GetItem<Person>(u => u.CreatedBy.Equals(userId.ToString()));

                    var userRoles = new List<string>();
                    if (user.Roles != null && user.Roles.Length > 0)
                    {
                        userRoles.AddRange(user.Roles.ToList());
                    }

                    foreach (var role in roles)
                    {
                        var roleMap = _repository.GetItem<UserRoleMap>(
                            u => u.UserId.Equals(userId.ToString()) && u.RoleName.Equals(role)
                        );

                        if (!string.IsNullOrEmpty(roleMap?.RoleName))
                        {
                            _repository.Delete<UserRoleMap>(
                                u => u.UserId.Equals(userId.ToString()) && u.RoleName.Equals(role)
                            );

                            _logger.LogInformation("{LogMessageSuccess} UnassignRoleFromUser (Remove from UserRoleMap) :: userId -> {UserId}, role -> {Role}",
                                LogHelpers.LogMessageSuccess, userId, role);
                        }

                        userRoles.Remove(role);
                    }

                    var userUpdate = new Dictionary<string, object>
                    {
                        { "Roles", userRoles.Distinct().ToArray() }
                    };
                    var personUpdate = new Dictionary<string, object>
                    {
                        { "Roles", userRoles.Distinct().ToArray() }
                    };

                    _mongoClientRepository.Update(EntityNames.User, user.ItemId, userUpdate);
                    _logger.LogInformation("{LogMessageSuccess} UnassignRoleFromUser :: userId -> {UserId}, roles -> {Roles}",
                        LogHelpers.LogMessageSuccess, user.ItemId, roles);

                    _mongoClientRepository.Update(EntityNames.Person, person.ItemId, personUpdate);
                    _logger.LogInformation("{LogMessageSuccess} UnassignRoleFromUser from Person :: personId -> {PersonId}, roles -> {Roles}",
                        LogHelpers.LogMessageSuccess, person.ItemId, roles);
                }
                else
                {
                    _logger.LogInformation("{LogMessageFail} User Get Null, AssignRoleToUser :: userId -> {UserId}, {Roles}",
                       LogHelpers.LogMessageFail, userId, LogHelpers.JsonToString("roles", roles));
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "{LogMessageException} UnassignRoleFromUser :: userId -> {UserId}, roles -> {Roles}",
                    LogHelpers.LogMessageException, userId, roles);
            }
        }
    }
}