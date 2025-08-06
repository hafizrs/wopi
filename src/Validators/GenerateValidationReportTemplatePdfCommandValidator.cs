using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class GenerateValidationReportTemplatePdfCommandValidator : AbstractValidator<GenerateValidationReportTemplatePdfCommand>
    {
        public GenerateValidationReportTemplatePdfCommandValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(x => x.FileNameWithExtension)
                .NotEmpty()
                .WithMessage("File name with extension is required.");
            RuleFor(x => x.ReportFileId)
                .NotEmpty()
                .WithMessage("Report file ID is required.");
            //RuleFor(x => x.HeaderHtmlFileId)
            //    .NotEmpty()
            //    .WithMessage("Header HTML file ID is required.");
            //RuleFor(x => x.FooterHtmlFileId)
            //    .NotEmpty()
            //    .WithMessage("Footer HTML file ID is required.");
            RuleFor(x => x.TemplateFileId)
                .NotEmpty()
                .WithMessage("Template file ID is required.");
            RuleFor(x => x.FilterString)
                .NotEmpty()
                .WithMessage("Filter string is required.");
        }

        public bool IsSatisfiedBy(GenerateValidationReportTemplatePdfCommand command)
        {
            var result = Validate(command);
            return result.IsValid;
        }
    }
}
