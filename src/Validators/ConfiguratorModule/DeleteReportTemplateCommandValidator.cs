using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;


namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class DeleteReportTemplateCommandValidator : AbstractValidator<DeleteReportTemplateCommand>
    {
        public DeleteReportTemplateCommandValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command cannot be null.");
            RuleFor(x => x.TemplateIds)
                .NotEmpty()
                .WithMessage("TemplateId is required.");
        }

        public ValidationResult IsSatisfiedBy(DeleteReportTemplateCommand command)
        {
            var validationResult = Validate(command);
            return !validationResult.IsValid ? validationResult : new ValidationResult();
        }
    }
}
