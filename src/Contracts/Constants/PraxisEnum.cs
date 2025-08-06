namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public enum PraxisEnums
    {
        INITIATED,
        ONGOING,
        EXPIRED,
        UNAUTHORIZE,
        AUTHORIZE
    }
    public enum PraxisSubscriptionPackage
    {
        RQ_MONITOR,
        PROCESS_GUIDE,
        COMPLETE_PACKAGE
    }

    public enum RiqsIncidentTopicEnums
    {
        PATIENT_SAFETY,
        EMPLOYEES,
        REPUTATION,
        ECONOMICS,
        OTHERS
    }

    public enum SortDirectionEnum
    {
        Ascending = 1,
        Descending = -1
    }
    public enum TwoFactorType
    {
        System,
        Anonymous
    }
}
