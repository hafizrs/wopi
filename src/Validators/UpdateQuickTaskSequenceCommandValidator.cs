using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdateQuickTaskSequenceCommandValidator : AbstractValidator<UpdateQuickTaskSequenceCommand>
    {
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;
        public UpdateQuickTaskSequenceCommandValidator(IQuickTaskPermissionService quickTaskPermissionService)
        {
            _quickTaskPermissionService = quickTaskPermissionService;
            RuleFor(command => command.QuickTaskIds)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("QuickTaskIds can't be null.")
                .NotEmpty().WithMessage("QuickTaskIds can't be empty.");
        }

        public ValidationResult IsSatisfiedby(UpdateQuickTaskSequenceCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 