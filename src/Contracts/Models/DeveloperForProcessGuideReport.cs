using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class DeveloperForProcessGuideReport
    {
        public string Title { get; set; }
        public string CreatedOn { get; set; }
        public string Topic { get; set; }
        public List<AssignOrganization> AssignOrganizations { get; set; }
    }
    public class AssignOrganization
    {
        public string ClientName { get; set; }
        public List<TaskDescriptionDto> TaskDescriptions { get; set; }
    }
    public class TaskDescriptionDto
    {
        public string TaskTitle { get; set; }
        public string Attachment { get; set; }
        public double Budget { get; set; }

        public TaskDescriptionDto()
        {
            TaskTitle = string.Empty;
            Attachment = string.Empty;
            Budget = 0;
        }
    }
}
