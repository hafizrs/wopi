namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public class DynamicRolePrefix
    {
        protected DynamicRolePrefix() { }

        public const string User = "user";
        public const string PraxisClientAdmin = "client-admin"; // client access -> read, edit
        public const string PraxisClientRead = "client-read"; // read-only
        public const string PraxisClientManager = "client-manager"; // child of client edit
        public const string PraxisEEGroup1 = "client-mpa-group-1";
        public const string PraxisEEGroup2 = "client-mpa-group-2";

        public const string Open_Organization = "poweruser-open-org";
        public const string Audit_Safe = "poweruser-audit-safe";

        public const string PraxisUser = "psuser";
        public const string PraxisForm = "psform";
        public const string PraxisTask = "pstask";
        public const string PraxisTaskConfig = "pstaskconfig";
    }
}
