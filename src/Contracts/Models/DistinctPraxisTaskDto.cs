using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class DistinctPraxisTaskDto
    {
        [BsonId]
        public string ItemId { get; set; }
        public string PraxisTaskId { get; set; }
        public string TaskConfigId { get; set; }
        public string ClientId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public List<string> ControllingMembers { get; set; }
        public List<string> ControlledMembers { get; set; }
        public string Remarks { get; set; }
        public DateTime? TaskDateTime { get; set; }
        public double? TaskPercentage { get; set; }
        public TaskSummaryDto TaskSummary { get; set; }
    }
}
