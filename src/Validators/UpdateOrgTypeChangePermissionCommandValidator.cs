using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
   public class UpdateOrgTypeChangePermissionCommandValidator : AbstractValidator<UpdateOrgTypeChangePermissionCommand>
    {
        public UpdateOrgTypeChangePermissionCommandValidator()
        {
            RuleFor(command => command.ClientId)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("ClientId can't be null.")
                            .NotEmpty().WithMessage("ClientId can't be empty.");
        }
        public ValidationResult IsSatisfiedby(UpdateOrgTypeChangePermissionCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
