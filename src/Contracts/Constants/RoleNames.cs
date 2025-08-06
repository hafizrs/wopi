namespace Selise.Ecap.SC.Wopi.Contracts.Constants
{
    public class RoleNames
    {
        protected RoleNames() { }

        public const string Admin = "admin";
        public const string AppUser = "appuser";
        public const string Anonymous = "anonymous";
        public const string ExternalUser = "external_user";
        public const string SystemAdmin = "system_admin";
        public const string TaskController = "task_controller";
        public const string PowerUser = "poweruser";
        public const string TechnicalClient = "technical_client";
        public const string ClientSpecific = "client_specific";
        public const string Tenantadmin = "tenantadmin";
        public const string AdminB = "super-poweruser";
        public const string GroupAdmin = "group-super-poweruser";

        //Payment Role
        public const string PoweruserPayment = "poweruser-payment";

        // Controlling Group
        public const string Leitung = "leitung";

        // Controlled Group
        public const string Mpa = "mpa";
        public const string MpaGroup1 = "mpa-group-1";
        public const string MpaGroup2 = "mpa-group-2";

        //Dynamic Role
        public const string AdminB_Dynamic = "org-admin";
        public const string PowerUser_Dynamic = "client-admin";
        public const string Leitung_Dynamic = "client-manager";
        public const string MpaGroup_Dynamic = "client-read";
        public const string Organization_Read_Dynamic = "org-read";

        //Navigation Role
        public const string PowerUser_Nav = "poweruser-nav";
        public const string Leitung_Nav = "leitung-nav";
        public const string MpaGroup1_Nav = "mpa-group-1-nav";
        public const string MpaGroup2_Nav = "mpa-group-2-nav";

        //Double Roles
        public const string PowerUser_Leitung = "poweruser-leitung";
        public const string PowerUser_MpaGroup1 = "poweruser-mpa-group-1";
        public const string PowerUser_MpaGroup2 = "poweruser-mpa-group-2";
        public const string Leitung_MpaGroup1 = "leitung-mpa-group-1";
        public const string Leitung_MpaGroup2 = "leitung-mpa-group-2";
        public const string MpaGroup1_MpaGroup2 = "mpa-group-1-mpa-group-2";
        public const string Poweruser_Payment = "poweruser-payment";

        //Triple Roles
        public const string PowerUser_Leitung_MpaGroup1 = "poweruser-leitung-mpa-group-1";
        public const string PowerUser_Leitung_MpaGroup2 = "poweruser-leitung-mpa-group-2";
        public const string PowerUser_MpaGroup1_MpaGroup2 = "poweruser-mpa-group-1-mpa-group-2";
        public const string Leitung_MpaGroup1_MpaGroup2 = "leitung-mpa-group-1-mpa-group-2";

        //All Roles
        public const string PowerUser_Leitung_MpaGroup1_MpaGroup2 = "poweruser-leitung-mpa-group-1-mpa-group-2";

        //Persona Role Name
        public const string Persona_Poweruser_Payment = "pay-persona";
        public const string Persona_PowerUser = "p-persona";
        public const string Persona_Leitung = "l-persona";
        public const string Persona_MpaGroup1 = "mg1-persona";
        public const string Persona_MpaGroup2 = "mg2-persona";

        public const string Persona_PowerUser_Leitung = "pl-persona";
        public const string Persona_PowerUser_MpaGroup1 = "pmg1-persona";
        public const string Persona_PowerUser_MpaGroup2 = "pmg2-persona";
        public const string Persona_Leitung_MpaGroup1 = "lmg1-persona";
        public const string Persona_Leitung_MpaGroup2 = "lmg2-persona";
        public const string Persona_MpaGroup1_MpaGroup2 = "mg1mg2-persona";

        public const string Persona_PowerUser_Leitung_MpaGroup1 = "plmg1-persona";
        public const string Persona_PowerUser_Leitung_MpaGroup2 = "plmg2-persona";
        public const string Persona_PowerUser_MpaGroup1_MpaGroup2 = "pmg1mg2-persona";
        public const string Persona_Leitung_MpaGroup1_MpaGroup2 = "lmg1mg2-persona";

        public const string All_Roles = "plmg1mg2-persona";

        public const string Open_Organization = "poweruser-open-org";
        public const string Audit_Safe = "poweruser-audit-safe";

        public const string Deactivate_Account = "user-account-deactivate";

    }
}
