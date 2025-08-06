using Selise.Ecap.Entities.PrimaryEntities.Security;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona
{
    public class PersonaRolesService : IPersonaRolesService
    {
        public PersonaRole[] GetPersonaRolesByUserRole(string role, string personaName, string departmentId, string organizationId)
        {
            var roles = new List<string>();
            var personRoleList = new List<PersonaRole>();

            switch (role.ToLower())
            {
                case RoleNames.PoweruserPayment:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add($"{RoleNames.PoweruserPayment}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.Leitung:
                    roles.Add(RoleNames.Leitung);
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.MpaGroup1:
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.MpaGroup2:
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser_Leitung:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add(RoleNames.Leitung);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser_MpaGroup1:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser_MpaGroup2:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.Leitung_MpaGroup1:
                    roles.Add(RoleNames.Leitung);
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.Leitung_MpaGroup2:
                    roles.Add(RoleNames.Leitung);
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.MpaGroup1_MpaGroup2:
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser_Leitung_MpaGroup1:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add(RoleNames.Leitung);
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser_Leitung_MpaGroup2:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add(RoleNames.Leitung);
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser_MpaGroup1_MpaGroup2:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.Leitung_MpaGroup1_MpaGroup2:
                    roles.Add(RoleNames.Leitung);
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                case RoleNames.PowerUser_Leitung_MpaGroup1_MpaGroup2:
                    roles.Add(RoleNames.PowerUser);
                    roles.Add(RoleNames.Leitung);
                    roles.Add(RoleNames.MpaGroup1);
                    roles.Add(RoleNames.MpaGroup2);
                    roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.PowerUser_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.Leitung_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup1_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.MpaGroup2_Nav}_{departmentId}");
                    roles.Add($"{RoleNames.Organization_Read_Dynamic}_{organizationId}");
                    roles.Add(personaName);
                    roles.Add(RoleNames.ClientSpecific);
                    break;
                default:
                    break;
            }
            foreach (var roleName in roles)
            {
                var PersonaRole = new PersonaRole { RoleName = roleName, IsOptional = false };
                personRoleList.Add(PersonaRole);
            }
            return personRoleList.ToArray();
        }
    }
}
