using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class LibraryFormFillActionDetailModel
    {
        public string PerformedBy { get; set; }
        public string PerformerName { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public DateTime PerformedOn { get; set; }
    }

}