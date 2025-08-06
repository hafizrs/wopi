using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class EditQuickTaskCommandValidator : AbstractValidator<EditQuickTaskCommand>
    {
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;
        public EditQuickTaskCommandValidator(IQuickTaskPermissionService quickTaskPermissionService)
        {
            _quickTaskPermissionService = quickTaskPermissionService;
            RuleFor(command => command.ItemId)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Id for edit record can't be null.")
               .NotEmpty().WithMessage("Id for edit can't be empty.")
               .Must((quickTaskId) => _quickTaskPermissionService.HasQuickTaskPlanDepartmentPermission(quickTaskId))
               .WithMessage("User does not have permission to update quick task for selected department");
            RuleFor(command => command.TaskGroupName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Task group name can't be null.");
            RuleFor(command => command.TaskList)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("TaskList can't be null.")
                .NotEmpty().WithMessage("TaskList can't be empty.");
        }

        public ValidationResult IsSatisfiedby(EditQuickTaskCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 