using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdateProcessGuideCompletionStatusCommandValidator: AbstractValidator<UpdateProcessGuideCompletionStatusCommand>
    {
        public UpdateProcessGuideCompletionStatusCommandValidator()
        {
            RuleFor(command => command.ProcessGuideIds)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty();
        }
        public ValidationResult IsSatisfiedBy(UpdateProcessGuideCompletionStatusCommand command)
        {
            return Validate(command);
        }
    }
}