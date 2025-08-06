using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ProcessGuideCaseOverviewReportModel
    {
        public string Title { get; set; }
        public string CaseNo { get; set; }
        public string BirthDay { get; set; }
        public string Name { get; set; }
        public string AssignedOn { get; set; }
        public List<TaskAssignDetails> TaskDetails { get; set; }
        public string Status { get; set; }
        public string FormTitle { get; set; }
        public string FormDescription { get; set; }
        public string Shifts { get; set; }
        public string Topic { get; set; }
    }

    public class TaskAssignDetails
    {
        public string ClientName { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string AssignedUsers { get; set; }
        public string CompletedUsers { get; set; }
        public string DueDate { get; set; }
        public int CompletionStatus { get; set; }
        public string DateOfCompletion { get; set; }
        public double Budget { get; set; }
        public double EffectiveCost { get; set; }
    }
}
