using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona
{
    public class RoleHierarchyForPersonaRoleService : IRoleHierarchyForPersonaRoleService
    {
        private static readonly string[] systemAdmins = new[] { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.TaskController };
        private static readonly string[] poweruserLeitungParents = new [] { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.TaskController, RoleNames.PowerUser};
        private static readonly string[] mPaGroupParents= new[] { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung };

        public string[] GetParentList(string role)
        {
            return role.ToLower() switch
            {
                RoleNames.AdminB => systemAdmins,
                RoleNames.Organization_Read_Dynamic => systemAdmins,
                RoleNames.Poweruser_Payment => poweruserLeitungParents,
                RoleNames.PowerUser => poweruserLeitungParents,
                RoleNames.Leitung => poweruserLeitungParents,
                RoleNames.MpaGroup1 => mPaGroupParents,
                RoleNames.MpaGroup2 => mPaGroupParents,
                RoleNames.PowerUser_Leitung => poweruserLeitungParents,
                RoleNames.PowerUser_MpaGroup1 => mPaGroupParents,
                RoleNames.PowerUser_MpaGroup2 => mPaGroupParents,
                RoleNames.Leitung_MpaGroup1 => mPaGroupParents,
                RoleNames.Leitung_MpaGroup2 => mPaGroupParents,
                RoleNames.MpaGroup1_MpaGroup2 => mPaGroupParents,
                RoleNames.PowerUser_Leitung_MpaGroup1 => mPaGroupParents,
                RoleNames.PowerUser_Leitung_MpaGroup2 => mPaGroupParents,
                RoleNames.PowerUser_MpaGroup1_MpaGroup2 => mPaGroupParents,
                RoleNames.Leitung_MpaGroup1_MpaGroup2 => mPaGroupParents,
                RoleNames.PowerUser_Leitung_MpaGroup1_MpaGroup2 => mPaGroupParents,
                _ => new string[] { },
            };
        }
    }
}
