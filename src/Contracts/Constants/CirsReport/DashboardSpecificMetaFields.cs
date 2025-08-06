namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport
{
    public enum ReportingVisibility
    {
        All,
        Management,
        Officer
    }

    public enum CirsBooleanEnum
    {
        False = 0,
        True = 1
    }

    public enum CommonCirsMetaKey
    {
        DecisionSelection,
        DecisionSelectionReason,
        ReportExternalOffice,
        ReportInternalOffice,
        ResponseText,
        ReporterClientId,
        ReportingVisibility,
        CirsParentId,
        IsSentAnonymously
    }
    public enum IdeaMetaKey
    {
        BenefitOfIdea,
        Requirements,
        FeasibilityAndResourceRequirements,
        TargetGroup,
        Options
    }

    public enum ComplainMetaKey
    {
    }

    public enum AnotherMetaKey
    {
        ImplementationProposal
    }

    public enum IncidentMetaKey
    {
        Topic,
        Measures,
    }

    public enum HintMetaKey
    {
        ReportingDate
    }
}
