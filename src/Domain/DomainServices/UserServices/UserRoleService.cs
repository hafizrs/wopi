using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.UserServices
{
    public class UserRoleService : IUserRoleService
    {
        private readonly IPreparePersonaRoleMap _preparePersonaRoleMapService;
        private readonly IProcessOpenOrgRole _processOpenOrgRoleService;
        private readonly IRepository _repository;
        private readonly ILogger<PreparePersonaRoleMapService> _logger;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForPersonaRoleService;
        private readonly IMongoSecurityService _ecapSecurityService;

        public UserRoleService(
            IPreparePersonaRoleMap preparePersonaRoleMapService,
            IProcessOpenOrgRole processOpenOrgRoleService,
            IRepository repository,
            ILogger<PreparePersonaRoleMapService> logger,
            IRoleHierarchyForPersonaRoleService roleHierarchyForPersonaRoleService,
            IMongoSecurityService ecapSecurityService
        )
        {
            _preparePersonaRoleMapService = preparePersonaRoleMapService;
            _repository = repository;
            _processOpenOrgRoleService = processOpenOrgRoleService;
            _repository = repository;
            _logger = logger;
            _roleHierarchyForPersonaRoleService = roleHierarchyForPersonaRoleService;
            _ecapSecurityService = ecapSecurityService;
        }

        public List<string> GetPersonaRoles(List<PraxisClientInfo> clientList, List<PaymentClientRelation> paymentClientRelation = null)
        {
            var userInformations = clientList.Select(
                    clientData => new UserInformation
                    {
                        ClientId = clientData.ClientId,
                        OrganizationId = clientData.ParentOrganizationId,
                        UserRoles = clientData.Roles.ToArray(),
                        IsPaymentPowerUser = paymentClientRelation?.Any(pcr => pcr.ClientId == clientData.ClientId) ??
                                             false
                    }).ToList();

            var getPersonaRolesQueryModel = new GetPersonaRolesQuery { UserInformations = userInformations };
            var personaRolesResponse = _preparePersonaRoleMapService.GeneratePersonaRoles(getPersonaRolesQueryModel);

            var personaRoles = new List<string>();
            personaRoles.AddRange(personaRolesResponse.DeleteFeatureRoles);
            personaRoles.AddRange(personaRolesResponse.PersonaRoles);

            return personaRoles;
        }

        public List<string> PrepareAdminBRoles(List<PraxisClientInfo> clientList, bool isGroupAdmin = false)
        {
            List<string> adminBRoles = new List<string>()
            {
                RoleNames.AdminB,
                RoleNames.ClientSpecific
            };

            List<string> distinctOrgIds = clientList.Select(c => c.ParentOrganizationId).Distinct().ToList();

            distinctOrgIds.ForEach((id) =>
            {
                string personaRoleName = _preparePersonaRoleMapService.PrepareDynamicRoleForPersona(RoleNames.AdminB, id);
                adminBRoles.Add(personaRoleName);

                List<PraxisClientInfo> departments = clientList.Where(c => c.ParentOrganizationId == id).ToList();
                var departmentIds = departments.Select(c => c.ClientId).ToList();

                List<string> deleteFeatureRoles = new List<string>();
                departmentIds.ForEach((id) =>
                {
                    deleteFeatureRoles.Add(GetDeleteFeatureRole(id));
                });
                adminBRoles.AddRange(deleteFeatureRoles);
            });

            if (isGroupAdmin)
            {
                adminBRoles.Add(RoleNames.GroupAdmin);
            }
            return adminBRoles;
        }

        public List<string> GetOrganizationWideRoles(List<PraxisClientInfo> clientList)
        {
            List<string> roles = new List<string>();

            List<string> distinctOrgIds = clientList.Select(c => c.ParentOrganizationId).Distinct().ToList();
            distinctOrgIds.RemoveAll(id => string.IsNullOrWhiteSpace(id));

            distinctOrgIds.ForEach((id) =>
            {
                roles.Add($"{RoleNames.Organization_Read_Dynamic}_{id}");
            });

            roles.ForEach((role) =>
            {
                CreateRole(role, true, RoleNames.Organization_Read_Dynamic);
            });

            return roles;
        }

        public string GetDeleteFeatureRole(string clientId)
        {
            var praxisClientData = _repository.GetItem<PraxisClient>(x => x.ItemId == clientId);
            return _processOpenOrgRoleService.ProcessRole(clientId, praxisClientData.IsOpenOrganization.Value).Role;
        }

        public string CreateRole(string role, bool isDynamic, string staticRole)
        {
            _logger.LogInformation("Going to create role: {Role}.", role);
            try
            {
                if (!_ecapSecurityService.IsRoleExist(role))
                {
                    _ecapSecurityService.CreateRole(role, isDynamic);
                }
                var isExistRoleHierarchy = _repository.ExistsAsync<RoleHierarchy>(h => h.Role == role).Result;
                if (!isExistRoleHierarchy)
                {
                    var parents = _roleHierarchyForPersonaRoleService.GetParentList(staticRole);
                    var newRoleHierarchy = new RoleHierarchy
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Parents = parents.ToList(),
                        Role = role
                    };
                    _repository.Save(newRoleHierarchy);
                    _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with ItemId: {ItemId}.", nameof(RoleHierarchy), newRoleHierarchy.ItemId);
                }
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred while creating new role: {Role}. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    role, ex.Message, ex.StackTrace);
                return string.Empty;
            }
        }
    }
}