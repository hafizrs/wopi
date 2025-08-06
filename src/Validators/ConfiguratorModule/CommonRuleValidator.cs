using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public static class CommonRuleValidator
    {
        public static InlineValidator<ReportTemplateSectionElement> CreateCommonSectionElementValidator()
        {
            var validator = new InlineValidator<ReportTemplateSectionElement>();
            ApplyCommonReportTemplateSectionElementRule(validator);
            return validator;
        }

        public static void ApplyCommonReportTemplateSectionElementRule(InlineValidator<ReportTemplateSectionElement> sectionElement)
        {
            sectionElement.RuleFor(element => element.ElementType)
                        .IsInEnum()
                        .WithMessage("ElementType must be a valid enum value");
            
            sectionElement.RuleFor(element => element.SequenceNo)
                .GreaterThan(0)
                .WithMessage("SequenceNo must be greater than 0");

            sectionElement.When(e => e.ElementType == ElementType.Question, () =>
            {
                sectionElement.RuleFor(e => e.QuestionType)
                    .IsInEnum()
                    .WithMessage("QuestionType must be a valid enum value when ElementType is Question");

                sectionElement.RuleFor(e => e.QuestionOptions)
                    .NotEmpty()
                    .WithMessage("At least one QuestionOption is required when ElementType is Question");
            });

            //sectionElement.When(e => e.ElementType == ElementType.ModuleInformation, () =>
            //{
            //    sectionElement.RuleFor(e => e.ModuleName)
            //        .NotEmpty()
            //        .WithMessage("ModuleName can't be null or empty when ElementType is ModuleInformation");

            //    sectionElement.RuleFor(e => e.ModuleId)
            //        .NotEmpty()
            //        .WithMessage("ModuleId can't be null or empty when ElementType is ModuleInformation")
            //        .Must(id => Guid.TryParse(id, out _))
            //        .WithMessage("ModuleId must be a valid GUID when ElementType is ModuleInformation");
            //});

            sectionElement.When(e => e.ElementType == ElementType.Image, () =>
            {
                sectionElement.RuleFor(e => e.Images)
                    .NotNull();
            });

            sectionElement.When(e => e.ElementType == ElementType.Table || e.ElementType == ElementType.HeaderText, () =>
            {
                sectionElement.RuleFor(e => e.InnerHtml)
                    .NotEmpty()
                    .WithMessage("InnerHtml can't be null or empty when ElementType is Table");
            });

            sectionElement.When(e => e.ElementType == ElementType.Summary || e.ElementType == ElementType.FreeText, () =>
            {
                sectionElement.RuleFor(e => e.Description)
                    .NotEmpty()
                    .WithMessage("Description can't be null or empty when ElementType is Summary");
            });
        }
    }
}
