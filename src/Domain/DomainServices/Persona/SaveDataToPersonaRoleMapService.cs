using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using System;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona
{
    public class SaveDataToPersonaRoleMapService : ISaveDataToPersonaRoleMap
    {
        private readonly IRepository _repository;
        private readonly ILogger<SaveDataToPersonaRoleMapService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IPersonaRolesService _personaRolesService;
        private readonly IProcessOpenOrgRole _processOpenOrgRoleService;

        public SaveDataToPersonaRoleMapService(
            IRepository repository,
            ILogger<SaveDataToPersonaRoleMapService> logger,
            ISecurityContextProvider securityContextProvider,
            IPersonaRolesService personaRolesService,
            IProcessOpenOrgRole processOpenOrgRoleService)
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _personaRolesService = personaRolesService;
            _processOpenOrgRoleService = processOpenOrgRoleService;
        }

        public (bool, string) SaveDataToPersonaRoleMapTable(
            string role,
            string personaName,
            bool IsPowerUser,
            bool isPaymentPowerUser,
            string departmentId,
            string organizationId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            _logger.LogInformation("Going to save {EntityName} entity with PersonaRole: {PersonaRole} and DepartmentId: {DepartmentId} and TenantId: {TenantId}", nameof(PersonaRoleMap), personaName, departmentId, securityContext.TenantId);
            try
            {
                var openOrgRole = string.Empty;
                var roleExist = _repository.ExistsAsync<PersonaRoleMap>(r => r.Persona == personaName).Result;
                if (!roleExist)
                {
                    if (IsPowerUser)
                    {
                        var client = _repository.GetItem<PraxisClient>(c => c.ItemId == departmentId);
                        if (client != null)
                        {
                            var response = _processOpenOrgRoleService.ProcessRole(departmentId, client.IsOpenOrganization.Value);
                            if (response.StatusCode == 200)
                            {
                                openOrgRole = response.Role;
                            }
                            else
                            {
                                return (false, string.Empty);
                            }
                        }
                    }

                    var personaRoles = _personaRolesService.GetPersonaRolesByUserRole(role, personaName, departmentId, organizationId);

                    if (!string.IsNullOrEmpty(openOrgRole))
                    {
                        var openOrgRoleName = new PersonaRole { RoleName = openOrgRole, IsOptional = false };
                        var finalPersonaRoles = personaRoles.ToList();
                        finalPersonaRoles.Add(openOrgRoleName);
                        personaRoles = finalPersonaRoles.ToArray();
                    }
                    var personaRoleMap = new PersonaRoleMap
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreatedBy = securityContext.UserId,
                        CreateDate = DateTime.Now.ToLocalTime(),
                        Language = "en-us",
                        LastUpdatedBy = securityContext.UserId,
                        LastUpdateDate = DateTime.Now.ToLocalTime(),
                        TenantId = securityContext.TenantId,
                        Persona = personaName,
                        PersonaRoles = personaRoles
                    };
                    _repository.Save(personaRoleMap);
                    _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with role name: {RoleName} and DepartmentId: {DepartmentId} and TenantId: {TenantId}", nameof(PersonaRoleMap), personaName, departmentId, securityContext.TenantId);
                }
                else
                {
                    if (IsPowerUser)
                    {
                        var client = _repository.GetItem<PraxisClient>(c => c.ItemId == departmentId);
                        if (client != null)
                        {
                            var response = _processOpenOrgRoleService.ProcessRole(departmentId, client.IsOpenOrganization.Value);
                            if (response.StatusCode == 200)
                            {
                                openOrgRole = response.Role;
                            }
                            else
                            {
                                return (false, string.Empty);
                            }
                        }
                    }
                }

                return (true, openOrgRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during save {EntityName} entity with PersonaRole: {PersonaRole} and DepartmentId: {DepartmentId} and TenantId: {TenantId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PersonaRoleMap), personaName, departmentId, securityContext.TenantId, ex.Message, ex.StackTrace);
                return (false, string.Empty);
            }
        }

        public bool SavePersonaRoleMap(string personaName, PersonaRole[] personaRoles)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            _logger.LogInformation("Going to save {EntityName} entity with PersonaRole: {PersonaRole} and TenantId: {TenantId}", nameof(PersonaRoleMap), personaName, securityContext.TenantId);
            try
            {
                var roleExist = _repository.ExistsAsync<PersonaRoleMap>(r => r.Persona == personaName).Result;
                if (!roleExist)
                {
                    var personaRoleMap = new PersonaRoleMap
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreatedBy = securityContext.UserId,
                        CreateDate = DateTime.Now.ToLocalTime(),
                        Language = "en-us",
                        LastUpdatedBy = securityContext.UserId,
                        LastUpdateDate = DateTime.Now.ToLocalTime(),
                        TenantId = securityContext.TenantId,
                        Persona = personaName,
                        PersonaRoles = personaRoles
                    };
                    _repository.Save(personaRoleMap);
                    _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with role name: {RoleName} and TenantId: {TenantId}", nameof(PersonaRoleMap), personaName, securityContext.TenantId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during save {EntityName} entity with PersonaName: {PersonaName} and TenantId: {TenantId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PersonaRoleMap), personaName, securityContext.TenantId, ex.Message, ex.StackTrace);
                return false;
            }
        }
    }
}
