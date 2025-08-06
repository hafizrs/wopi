using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ProcessGuideDetailReport
    {
        public string Title { get; set; }
        public string CaseNo { get; set; }
        public string BirthDay { get; set; }
        public string Name { get; set; }
        public string AssignedOn { get; set; }
        public string Topic { get; set; }
        //[Setting, DefaultValue(default(List<TaskDescription>))]
        public List<TaskDescription> TaskDescriptions { get; set; }
        public int OverAllCompletion { get; set; }
        public string Status { get; set; }
        public string FormTitle { get; set; }
        public string FormDescription { get; set; }
        public string AdditionalDescription { get; set; }
        public List<string> Attachments { get; set; }
    }
    public class TaskDescription
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public int CompletionPercentage { get; set; }
        //[Setting, DefaultValue(default(List<TaskCompletedInfo>))]
        public List<TaskCompletedInfo> TaskCompletedInfos { get; set; }
    }
    public class TaskCompletedInfo
    {
        public string TaskTitle { get; set; }
        public string Budget { get; set; }
        public List<TaskCompletionRelatedInfo> TaskCompletionRelatedInfos { get; set; }
        public string AdditionalInformation { get; set; }

        public TaskCompletedInfo()
        {
            TaskTitle = string.Empty;
            Budget = string.Empty;
            TaskCompletionRelatedInfos = new List<TaskCompletionRelatedInfo>();
            AdditionalInformation = string.Empty;
        }
    }
    public class TaskCompletionRelatedInfo
    {
        public string CompletedBy { get; set; }
        public string DateOfCompletion { get; set; }
        public List<string> Attachments { get; set; }
        public string Remarks { get; set; }
        public double? EffectiveCost { get; set; }
        public TaskCompletionRelatedInfo()
        {
            CompletedBy = string.Empty;
            DateOfCompletion = string.Empty;
            Attachments = new List<string>();
            Remarks = string.Empty;
            EffectiveCost = 0;
        }
    }
    
}
