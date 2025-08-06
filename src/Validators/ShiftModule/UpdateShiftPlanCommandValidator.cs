using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdateShiftPlanCommandValidator : AbstractValidator<UpdateShiftPlanCommand>
    {
        private readonly IPraxisShiftPermissionService _praxisShiftPermissionService;
        public UpdateShiftPlanCommandValidator(IPraxisShiftPermissionService praxisShiftPermissionService)
        {
            _praxisShiftPermissionService = praxisShiftPermissionService;
            RuleFor(command => command.ShiftPlanIds)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ShiftPlanId can't be null.")
                .NotEmpty().WithMessage("ShiftPlanId Id can't be empty.")
                .Must(ids => ids.TrueForAll(id => _praxisShiftPermissionService.HasDepartmentPermissionGetByShiftplanId(id)))
                .WithMessage("User does not has permission to update shift plan for selected department");

            //RuleFor(command => command.PraxisUserIds)
            //    .Cascade(CascadeMode.StopOnFirstFailure)
            //    .NotNull().WithMessage("Praxis User Ids can't be null.")
            //    .NotEmpty().WithMessage("Praxis User Ids can't be empty.")
            //    .Must(list => list.All(id => !string.IsNullOrEmpty(id)))
            //    .WithMessage("Each Praxis User Id can't be null or empty.");
        }

        public ValidationResult IsSatisfiedby(UpdateShiftPlanCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
