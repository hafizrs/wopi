using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class DeleteShiftPlanCommandValidator : AbstractValidator<DeleteShiftPlanCommand>
    {
        private readonly IPraxisShiftPermissionService _praxisShiftPermissionService;
        public DeleteShiftPlanCommandValidator(IPraxisShiftPermissionService praxisShiftPermissionService)
        {
            _praxisShiftPermissionService = praxisShiftPermissionService;
            RuleFor(command => command.ShiftPlanIds)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("List of ShiftPlanIds cannot be null.")
            .NotEmpty().WithMessage("List of ShiftPlanIds cannot be empty.")
            .Must(ids => ids.TrueForAll(id => _praxisShiftPermissionService.HasDepartmentPermissionGetByShiftplanId(id)))
            .WithMessage("User does not have permission to update shift plans for one or more selected departments.");

        }

        public ValidationResult IsSatisfiedby(DeleteShiftPlanCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
