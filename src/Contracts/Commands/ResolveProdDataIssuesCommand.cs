using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ResolveProdDataIssuesCommand
    {
        public string Context { get; set; }
        public string FunctionName { get; set; }
        public ResolveProdDataIssuesPayload Payload { get; set; }
    }

    public class ResolveProdDataIssuesPayload
    {
        public List<string> OrganizationIds { get; set; }
        public List<string> DepartmentIds { get; set; }
        public double? AdditionalStorage {  get; set; }
        public double? AdditionalToken { get; set; }
        public double? AdditionalManualToken { get; set; }
        public List<UilmApplication> UilmApplications { get; set; } 
        public string UserId { get; set; }
        public int PageSize { get; set; } = 30;
        public int PageNumber { get; set; }
        public string TargetLanguage { get; set; }
        public string Filter { get; set; }
    }


}