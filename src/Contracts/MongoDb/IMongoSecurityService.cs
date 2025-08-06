using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb
{
    public interface IMongoSecurityService
    {
        string GetRoleName(string roleSlug, string itemId);

        User GetUserById(Guid userId);

        Person GetPersonByUserId(Guid userId);

        Person GetPersonById(Guid personId);

        bool IsRoleExist(string roleName);

        string CreateRole(string roleName, bool isDynamic);

        void AssignRoleToUser(Guid userId, List<string> roles, bool onlyActiveUser = true);

        void UnassignRoleFromUser(Guid userId, List<string> roles, bool onlyActiveUser = true);

        EntityAccessPermission GetRowLevelSecurity<T>(Guid entityId);

        void UpdateEntityReadWritePermission<T>(EntityReadWritePermission permission);

        void UpdateEntityReadWritePermission<T>(Guid entityId, EntityReadWritePermission permission);

        void UpdateRowLevelSecurityOfConnection(string connectionId);

        void UpdateRowLevelSecurityOfConnection(string connectionId, EntityReadWritePermission entityReadWritePermission);

        EntityReadWritePermission GetUpdatedRowLevelSecurityPayloadOfConnection(string connectionId);

        EntityReadWritePermission GetUpdatedRowLevelSecurityPayloadOfConnection(string connectionId, EntityReadWritePermission entityReadWritePermission);
    }
}
