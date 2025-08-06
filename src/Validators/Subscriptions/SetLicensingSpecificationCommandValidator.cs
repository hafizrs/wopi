using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class SetLicensingSpecificationCommandValidator : AbstractValidator<SetLicensingSpecificationCommand>
    {
        public SetLicensingSpecificationCommandValidator()
        {
            RuleFor(command => command.OrganizationId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Organization Id can't be null")
                .NotEmpty().WithMessage("Organization Id can't be empty");
            RuleFor(command => command.FeatureId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("FeatureId can't be null")
                .NotEmpty().WithMessage("FeatureId can't be empty");
            RuleFor(command => command.UsageLimit)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("UsageLimit can't be null")
                .NotEmpty().WithMessage("UsageLimit can't be empty");
        }
        public ValidationResult IsSatisfiedby(SetLicensingSpecificationCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
