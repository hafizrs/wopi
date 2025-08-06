using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdatePraxisUserDtosCommandValidator: AbstractValidator<UpdatePraxisUserDtosCommand>
    {
        public UpdatePraxisUserDtosCommandValidator()
        {
            RuleFor(command => command.PraxisUserIds)
                .NotNull()
                .NotEmpty()
                .When(command => !command.UpdateAll);
            RuleFor(command => command.UpdateAll)
                .NotNull()
                .NotEqual(false)
                .When(command => command.PraxisUserIds == null || command.PraxisUserIds.Count == 0);
        }

        public ValidationResult IsSatisfiedBy(UpdatePraxisUserDtosCommand command)
        {
            return Validate(command);
        }
    }
}