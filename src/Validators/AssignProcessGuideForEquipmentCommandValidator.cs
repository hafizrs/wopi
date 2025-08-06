using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class AssignProcessGuideForEquipmentCommandValidator : AbstractValidator<AssignProcessGuideForEquipmentCommand>
    {
        public AssignProcessGuideForEquipmentCommandValidator()
        {
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command can't be null.");
            RuleFor(command => command.FormIds)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("FormId can't be null.")
                .NotEmpty()
                .WithMessage("FormId can't be empty.");
            RuleFor(command => command.EquipmentId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("EquipmentId can't be null.")
                .NotEmpty()
                .WithMessage("EquipmentId can't be empty.");
        }

        public ValidationResult IsSatisfiedBy(AssignProcessGuideForEquipmentCommand command)
        {
            var commandValidity = Validate(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}