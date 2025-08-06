using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RelatedEntityObject
    {
        public string ItemId { get; set; }
        public string[] Tags { get; set; }
        public string Language { get; set; }
        public string FormId { get; set; }
        public string FormName { get; set; }
        public string Title { get; set; }
        public string TopicKey { get; set; }
        public string TopicValue { get; set; }
        public string Description { get; set; }
        public object PatientId { get; set; }
        public string PatientName { get; set; }
        public bool IsActive { get; set; }
        public IEnumerable<string> ControlledMembers { get; set; }
        public List<object> ClientCompletionInfo { get; set; }
        public string PraxisProcessGuideConfigId { get; set; }
        public IEnumerable<ProcessGuideClientInfo> Clients { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public DateTime PatientDateOfBirth { get; set; }
        public object DueDate { get; set; }
        public string RelatedEntityId { get; set; }
        public string RelatedEntityName { get; set; }
        public IEnumerable<PraxisShift> Shifts { get; set; }
    }
}
