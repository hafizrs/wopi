using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdateQuickTaskPlanCommandValidator : AbstractValidator<UpdateQuickTaskPlanCommand>
    {
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;
        public UpdateQuickTaskPlanCommandValidator(IQuickTaskPermissionService quickTaskPermissionService)
        {
            _quickTaskPermissionService = quickTaskPermissionService;
            RuleFor(command => command.QuickTaskPlanIds)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("QuickTaskPlanIds can't be null.")
                .NotEmpty().WithMessage("QuickTaskPlanIds can't be empty.")
                .Must(ids => ids.TrueForAll(id => _quickTaskPermissionService.HasDepartmentPermissionGetByQuickTaskPlanId(id)))
                .WithMessage("User does not have permission to update quick task plan for selected department");
        }

        public ValidationResult IsSatisfiedby(UpdateQuickTaskPlanCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 