using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class DeleteQuickTaskCommandValidator : AbstractValidator<DeleteQuickTaskCommand>
    {
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;
        public DeleteQuickTaskCommandValidator(IQuickTaskPermissionService quickTaskPermissionService)
        {
            _quickTaskPermissionService = quickTaskPermissionService;
            RuleFor(command => command.QuickTaskId)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Id for deleting record can't be null.")
               .NotEmpty().WithMessage("Id for deleting can't be empty.")
               .Must((quickTaskId) => _quickTaskPermissionService.HasDepartmentPermissionGetByQuickTaskPlanId(quickTaskId))
               .WithMessage("User does not have permission to delete quick task for selected department");
        }

        public ValidationResult IsSatisfiedby(DeleteQuickTaskCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 