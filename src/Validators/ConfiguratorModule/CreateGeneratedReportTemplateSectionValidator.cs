using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class CreateGeneratedReportTemplateSectionValidator : AbstractValidator<CreateGeneratedReportTemplateSectionCommand>
    {
        public CreateGeneratedReportTemplateSectionValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command can't be null");
            When(command => command != null, () =>
            {
                RuleFor(command => command.Sections)
                    .NotEmpty()
                    .WithMessage("At least one SectionElement is required");
                RuleForEach(command => command.Sections)
                    .ChildRules(sectionElement =>
                    {
                        sectionElement.RuleFor(command => command.ItemId)
                            .NotEmpty()
                            .WithMessage("ItemId can't be null or empty")
                            .Must(id => Guid.TryParse(id, out _))
                            .WithMessage("ItemId must be a valid GUID");
                        sectionElement.RuleFor(command => command.TemplateId)
                            .NotEmpty()
                            .WithMessage("TemplateId can't be null or empty")
                            .Must(id => Guid.TryParse(id, out _))
                            .WithMessage("TemplateId must be a valid GUID");
                        
                        sectionElement.RuleFor(command => command.SequenceNo)
                            .GreaterThan(0)
                            .WithMessage("SequenceNo must be greater than 0");

                        sectionElement.RuleForEach(command => command.SectionElements)
                            .SetValidator(new GeneratedReportTemplateSectionElementValidator())
                            .When(command => command.SectionElements != null && command.SectionElements.Count > 0);
                    });
            });
        }

        public ValidationResult IsSatisfiedBy(CreateGeneratedReportTemplateSectionCommand command)
        {
            var commandValidity = Validate(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}
