using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ProcessGuideDetailsResponse
    {
        public PraxisProcessGuide ProcessGuide { get; set; }
        public IEnumerable<PraxisProcessGuideAnswer> ProcessGuideAnswers { get; set; }
        public List<ProcessGuideClientCompletion> ProcessGuideClientCompletionList { get; set; }
        public Dictionary<string, bool> Permissions { get; set; }
        public List<GuideQuestionAnswerPermission> QuestionAnswerPermissions { get; set; }
        public PraxisForm PraxisForm { get; set; }
        public List<PraxisDocument> AttachedDocuments { get; set; }

    }

    public class GuideQuestionAnswerPermission
    {
        public string PraxisClientId { get; set; }
        public string QuestionId { get; set; }
        public Dictionary<string, bool> Permission { get; set; }
    }
}