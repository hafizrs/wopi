using Selise.Ecap.SC.WopiMonitor.Contracts.Constants;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.Models.WopiModule;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;

namespace Selise.Ecap.SC.WopiMonitor.Domain.DomainServices.WopiModule
{
    public class WopiPermissionService : IWopiPermissionService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        
        public WopiPermissionService(IRepository repository, ISecurityContextProvider securityContextProvider)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
        }

        public bool HasDepartmentPermission(string departmentId)
        {
            if (IsSystemUser() || IsAdminUser())
            {
                return true;
            }
            var securityContext = _securityContextProvider.GetSecurityContext();
            var user = _repository.GetItem<PraxisUser>(u => u.UserId == securityContext.UserId);
            var department = user.ClientList.FirstOrDefault(client => client.ClientId == departmentId);
            if (department == null)
            {
                return false;
            }
            return department.Roles.Any(role =>
                role == RoleNames.Admin ||
                role == RoleNames.AdminB ||
                role == RoleNames.PowerUser ||
                role == RoleNames.Leitung);
        }

        public bool HasWopiSessionPermission(string sessionId)
        {
            var session = _repository.GetItem<WopiSession>(s => s.SessionId == sessionId);
            if (session == null)
            {
                return false;
            }
            return HasDepartmentPermission(session.DepartmentId);
        }

        private bool IsSystemUser()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return securityContext.Roles.Contains("system_admin");
        }
        
        private bool IsAdminUser()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return securityContext.Roles.Contains("admin");
        }
    }
} 