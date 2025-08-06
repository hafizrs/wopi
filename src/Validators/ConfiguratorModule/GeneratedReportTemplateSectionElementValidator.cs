using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class GeneratedReportTemplateSectionElementValidator : AbstractValidator<GeneratedReportTemplateSectionElement>
    {
        public GeneratedReportTemplateSectionElementValidator()
        {
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command can't be null");
            When(command => command != null, () =>
            {
                RuleFor(command => command.Answers)
                    .NotEmpty()
                    .WithMessage("Answers can't be null or empty")
                    .When(command => command.ElementType == ElementType.Question);
                
                RuleFor(command => command)
                .SetValidator(CommonRuleValidator.CreateCommonSectionElementValidator());
            });
        }
    }
}
