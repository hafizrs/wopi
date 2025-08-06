using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
   public class ProcessUserCreateUpdateValidator: AbstractValidator<ProcessUserCreateUpdateCommand>
    {
        public ProcessUserCreateUpdateValidator()
        {
            RuleFor(command => command.PraxisUserInformation)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("PraxisUserInformation can't be null.")
               .NotEmpty().WithMessage("PraxisUserInformation can't be empty.");

            RuleFor(command => command.PraxisUserInformation.Email)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Email can't be null.")
               .NotEmpty().WithMessage("Email can't be empty.");

            RuleFor(command => command.PraxisUserInformation.FirstName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("FirstName can't be null.")
               .NotEmpty().WithMessage("FirstName can't be empty.");

            RuleFor(command => command.PraxisUserInformation.LastName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("LastName can't be null.")
               .NotEmpty().WithMessage("LastName can't be empty.");
            RuleFor(command => command.PraxisUserInformation.ClientList)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("ClientList can't be null.")
               .NotEmpty().WithMessage("ClientList can't be empty.");
            RuleFor(command => command.PraxisUserInformation.DateOfBirth)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("DateOfBirth can't be null.")
               .NotEmpty().WithMessage("DateOfBirth can't be empty.");
            RuleFor(command => command.PraxisUserInformation.DisplayName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("DisplayName can't be null.")
               .NotEmpty().WithMessage("DisplayName can't be empty.");
        }
        public ValidationResult IsSatisfiedby(ProcessUserCreateUpdateCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
