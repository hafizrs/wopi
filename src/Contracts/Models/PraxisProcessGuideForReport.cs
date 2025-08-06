using System;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisProcessGuideForReport
    {
        public string FormName { get; set; }
        public string ClientNames { get; set; }
        public string PatientName { get; set; }
        public string PatientId { get; set; }
        public string TopicValue { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public double? Budget { get; set; }
        public double? EffectiveCost { get; set; }
        public DateTime PatientDateOfBirth { get; set; }
        public DateTime? DueDate { get; set; }
        public int CompletionStatus { get; set; }
        public DateTime? CompletionDate { get; set; }
        public IEnumerable<ProcessGuideClientInfoForReport> Clients { get; set; }

    }

    public class ProcessGuideClientInfoForReport
    {
        public string ClientName { get; set; }
        public string ClientId { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public int CompletionPercentage { get; set; }
        public IEnumerable<ProcessGuideTaskForReport> CheckList { get; set; }
        public string AssignedUsers { get; set; }
        public string CompletedUsers { get; set; }
        public double? Budget { get; set; }
        public double? EffectiveCost { get; set; }
    }

    public class ProcessGuideTaskForReport
    {
        public string TaskTitle { get; set; }
        public double? Budget { get; set; }
        public double? EffectiveCost { get; set; }
        public IEnumerable<PraxisDocument> Files { get; set; }
        public IEnumerable<PraxisProcessGuideSingleAnswerWithPraxisUserInfo> TaskResponseList  { get; set; }
    }
}