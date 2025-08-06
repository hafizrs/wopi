using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class DeleteQuickTaskPlanCommandValidator : AbstractValidator<DeleteQuickTaskPlanCommand>
    {
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;
        public DeleteQuickTaskPlanCommandValidator(IQuickTaskPermissionService quickTaskPermissionService)
        {
            _quickTaskPermissionService = quickTaskPermissionService;
            RuleFor(command => command.QuickTaskPlanIds)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("List of QuickTaskPlanIds cannot be null.")
            .NotEmpty().WithMessage("List of QuickTaskPlanIds cannot be empty.")
            .Must(ids => ids.TrueForAll(id => _quickTaskPermissionService.HasDepartmentPermissionGetByQuickTaskPlanId(id)))
            .WithMessage("User does not have permission to update quick task plans for one or more selected departments.");
        }

        public ValidationResult IsSatisfiedby(DeleteQuickTaskPlanCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 