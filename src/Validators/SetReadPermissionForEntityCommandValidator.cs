using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class SetReadPermissionForEntityCommandValidator: AbstractValidator<SetReadPermissionForEntityCommand>
    {
        public SetReadPermissionForEntityCommandValidator()
        {
            RuleFor(command => command.EntityName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .WithMessage("Value can't be empty.")
                .NotNull()
                .WithMessage("Value can't be null.");
        }

        public ValidationResult IsSatisfiedBy(SetReadPermissionForEntityCommand command)
        {
            var commandValidity = Validate(command);
            
            return commandValidity.IsValid ? new ValidationResult() : commandValidity;
        }
    }
}