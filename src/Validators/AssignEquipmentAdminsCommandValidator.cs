using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class AssignEquipmentAdminsCommandValidator: AbstractValidator<AssignEquipmentAdminsCommand>
    {
        public AssignEquipmentAdminsCommandValidator()
        {
            RuleFor(command => command.DepartmentId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("DepartmentId can't be null.")
                .NotEmpty()
                .WithMessage("DepartmentId can't be empty.");
           /* RuleFor(command => command.EquipmentId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("EquipmentId can't be null.")
                .NotEmpty()
                .WithMessage("EquipmentId can't be empty.");*/
        }

        public ValidationResult IsSatisfiedBy(AssignEquipmentAdminsCommand command)
        {
            var commandValidity = Validate(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

    }
}