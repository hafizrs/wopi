using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdatePraxisTaskStatusCommandValidator : AbstractValidator<UpdatePraxisTaskStatusCommand>
    {
        public UpdatePraxisTaskStatusCommandValidator()
        {
            RuleFor(command => command.TaskSummaryId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("TaskSummaryId required.");

            RuleFor(command => command.TaskStatus)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("TaskStatus is not valid.");
        }

        public ValidationResult IsSatisfiedby(UpdatePraxisTaskStatusCommand command)
        {
           var commandValidity = Validate(command);

           if (!commandValidity.IsValid) return commandValidity;

           return new ValidationResult();
        }
    }
}
