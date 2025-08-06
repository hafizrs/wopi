using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona
{
    public class PreparePersonaRoleMapService : IPreparePersonaRoleMap
    {
        private readonly IRepository _repository;
        private readonly ILogger<PreparePersonaRoleMapService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IPersonaRoleNameService _personaRoleNameService;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForPersonaRoleService;
        private readonly IMongoSecurityService _ecapSecurityService;
        private readonly ISaveDataToPersonaRoleMap _saveDataToPersonaRoleMapService;

        public PreparePersonaRoleMapService(
            IRepository repository,
            ILogger<PreparePersonaRoleMapService> logger,
            ISecurityContextProvider securityContextProvider,
            IPersonaRoleNameService personaRoleNameService,
            IRoleHierarchyForPersonaRoleService roleHierarchyForPersonaRoleService,
            IMongoSecurityService ecapSecurityService,
            ISaveDataToPersonaRoleMap saveDataToPersonaRoleMapService)
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _personaRoleNameService = personaRoleNameService;
            _roleHierarchyForPersonaRoleService = roleHierarchyForPersonaRoleService;
            _ecapSecurityService = ecapSecurityService;
            _saveDataToPersonaRoleMapService = saveDataToPersonaRoleMapService;
        }

        public PersonaRoleResponse GeneratePersonaRoles(GetPersonaRolesQuery query)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var personaRoleList = new List<string>();
            var openOrgRoles = new List<string>();
            foreach (var userInformation in query.UserInformations)
            {
                _logger.LogInformation("Going to save persona roles for OrganizationId: {OrganizationId} and roles: {UserRoles} and TenantId: {TenantId}", userInformation.ClientId, JsonConvert.SerializeObject(userInformation.UserRoles), securityContext.TenantId);

                var isPowerUser = userInformation.UserRoles.Contains("poweruser");

                string role;
                if (userInformation.IsPaymentPowerUser)
                {
                    role = "poweruser-payment";
                }
                else if (userInformation.UserRoles.Length == 1)
                {
                    role = userInformation.UserRoles[0];
                }
                else
                {
                    role = string.Join('-', userInformation.UserRoles).ToLower();
                }

                var personaRole = PrepareDynamicRoleForPersona(role, userInformation.ClientId);
                if (personaRole != string.Empty)
                {
                    var response = _saveDataToPersonaRoleMapService.SaveDataToPersonaRoleMapTable(
                        role,
                        personaRole,
                        isPowerUser,
                        userInformation.IsPaymentPowerUser,
                        userInformation.ClientId,
                        userInformation.OrganizationId);
                    if (response.Item1)
                    {
                        personaRoleList.Add(personaRole);
                        if (response.Item2 != string.Empty)
                        {
                            openOrgRoles.Add(response.Item2);
                        }
                    }
                }
            }

            if (query.UserInformations.Count == personaRoleList.Count)
            {
                return new PersonaRoleResponse
                {
                    StatusCode = 200,
                    Messages = new List<string>(),
                    PersonaRoles = personaRoleList,
                    DeleteFeatureRoles = openOrgRoles
                };
            }
            return new PersonaRoleResponse
            {
                StatusCode = 500,
                Messages = new List<string> { "Exception occured during process persona roles." },
                PersonaRoles = new List<string>(),
                DeleteFeatureRoles = new List<string>()
            };
        }

        public bool PrepareAdminBOrgPersonaRoleMap(
            string personaName,
            List<string> departmentIds,
            List<string> currentDynamicRoles
        )
        {
            var roles = new List<string>();
            var personRoles = new List<PersonaRole>();

            roles.Add(RoleNames.AdminB);
            roles.Add(RoleNames.PowerUser);
            roles.Add(RoleNames.ClientSpecific);
            roles.AddRange(currentDynamicRoles);

            departmentIds.ForEach((id) =>
                {
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{id}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{id}");
                }
            );

            foreach (var role in roles)
            {
                var PersonaRole = new PersonaRole { RoleName = role, IsOptional = false };
                personRoles.Add(PersonaRole);
            }

            var isPersonaRoleMapSaved = _saveDataToPersonaRoleMapService.SavePersonaRoleMap(personaName, personRoles.ToArray());

            return isPersonaRoleMapSaved;
        }

        public string PrepareDynamicRoleForPersona(string role, string clientId)
        {
            _logger.LogInformation("Going to prepare dynamic role for Persona feature with role: {Role} and clientId: {ClientId}", role, clientId);
            try
            {
                var personaRoleName = _personaRoleNameService.GetPersonaRoleName(role, clientId);

                var isRoleExists = _ecapSecurityService.IsRoleExist(personaRoleName);
                var personaRole = !isRoleExists ? _ecapSecurityService.CreateRole(personaRoleName, true) : personaRoleName;
                var isExist = _repository.ExistsAsync<RoleHierarchy>(h => h.Role == personaRole).Result;
                if (!isExist)
                {
                    var parents = _roleHierarchyForPersonaRoleService.GetParentList(role);
                    var newRoleHierarchy = new RoleHierarchy
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Parents = parents.ToList(),
                        Role = personaRole
                    };
                    _repository.Save(newRoleHierarchy);
                    _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with ItemId: {ItemId}", nameof(RoleHierarchy), newRoleHierarchy.ItemId);
                }
                return personaRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during prepare dynamic role for Persona feature with role: {Role} and clientId: {ClientId}. Exception Message: {Message}. Exception Details: {StackTrace}", role, clientId, ex.Message, ex.StackTrace);
                return string.Empty;
            }
        }
    }
}
