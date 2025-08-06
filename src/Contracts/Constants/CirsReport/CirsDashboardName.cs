using System.Runtime.Serialization;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport
{
    public enum CirsDashboardName
    {
        [EnumMember(Value = "Complain")] Complain,
        [EnumMember(Value = "Incident")] Incident,
        [EnumMember(Value = "Hint")] Hint,
        [EnumMember(Value = "Another")] Another,
        [EnumMember(Value = "Idea")] Idea,
        [EnumMember(Value = "Fault")] Fault
    }
}
