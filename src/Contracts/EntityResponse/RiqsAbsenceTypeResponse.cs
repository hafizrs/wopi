using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class RiqsAbsenceTypeResponse
    {
        public RiqsAbsenceTypeResponse() { }
        public RiqsAbsenceTypeResponse(RiqsAbsenceType absenceType)
        {
            ItemId = absenceType.ItemId;
            Type = absenceType.Type;
            Color = absenceType.Color;
            DepartmentId = absenceType.DepartmentId;
        }

        public string ItemId { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public string DepartmentId { get; set; }
    }
}