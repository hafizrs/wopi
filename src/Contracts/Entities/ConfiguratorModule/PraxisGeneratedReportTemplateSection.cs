using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule
{
    public class PraxisGeneratedReportTemplateSection : PraxisReportTemplateSection
    {
        [BsonElement("GeneratedSectionElements")]
        public new List<GeneratedReportTemplateSectionElement> SectionElements { get; set; }

    }

    public class GeneratedReportTemplateSectionElement : ReportTemplateSectionElement
    {
        public List<CustomAnswerModel> Answers { get; set; } = new();
        public List<string> PreviousSummaryIds { get; set; } = new();
        public List<DeviationDocument> DeviationDocuments { get; set; } = new();
    }

    public class CustomAnswerModel
    {
        public string OptionId { get; set; }
        public string ReasonForChoosing { get; set; }
    }

    public class DeviationDocument
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Abbreviation { get; set; }
        public bool IsCritical { get; set; }
        public string ProposedCorrection { get; set; }
        public string EventDescription { get; set; }
        public string Remarks { get; set; }
        public List<PraxisImage> Attachments { get; set; }
        public string AnswerId { get; set; }
        public int SequenceNo { get; set; }
    }

    public class GeneratedReportTemplateSectionElementOfPdf : GeneratedReportTemplateSectionElement
    {
        public object AdditionalInformationsObject => ConvertToObject(AdditionalInformations);
        private static object ConvertToObject(IDictionary<string, string> dictionary)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;

            // Create a sorted collection of key-value pairs
            var sortedPairs = dictionary
                .Select(kvp => new { Key = kvp.Key, Value = kvp.Value })
                .OrderBy(item => double.TryParse(item.Value, out double numValue) ? numValue : double.MaxValue)
                .ToList();

            // Add the sorted pairs to the expando object
            foreach (var item in sortedPairs)
            {
                expandoDict[item.Key] = item.Value;
            }

            return expando;
        }
    }
}
