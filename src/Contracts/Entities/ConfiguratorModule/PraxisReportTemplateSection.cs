using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule
{
    public class PraxisReportTemplateSection : EntityBase
    {
        public string TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int SequenceNo { get; set; }
        public bool IsEnableConditionalQuestions { get; set; }
        public List<ReportTemplateSectionElement> SectionElements { get; set; }
    }

    public class ReportTemplateSectionElement
    {
        public ElementType ElementType { get; set; }
        public QuestionType QuestionType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int SequenceNo { get; set; }
        public List<ReportTemplateQuestionOptions> QuestionOptions { get; set; }
        public bool IsRequired { get; set; }
        public string InnerHtml { get; set; }
        public List<List<IDictionary<string, object>>> InnerHtmlDict { get; set; }
        public string ModuleName { get; set; }
        public string ModuleId { get; set; }
        public List<PraxisImage> Images { get; set; }
        public bool EnableAttachments { get; set; } = false;
        public IDictionary<string, string> AdditionalInformations { get; set; } = new Dictionary<string, string>();
    }

    public class ReportTemplateQuestionOptions
    {
        public string OptionId { get; set; }
        public string Title { get; set; }
        public bool IsCorrectAnswer { get; set; } = false;
    }

    public enum ElementType
    {
        Question = 0,
        FreeText = 1,
        Image = 2,
        Table = 3,
        ModuleInformation = 4,
        Summary = 5,
        HeaderText = 6,
        CoverPage = 7,
        TableOfContent = 8,
        Signature = 9
    }
    public enum QuestionType
    {
        MultipleChoice = 0,
        RatingScale = 1,
        LikertScale = 2,
        OpenText = 3,
        DropDown = 4,
        Ranking = 5,
        Binary = 6,
        DateSelection = 7
    }
}
