using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class GetRiqsTranslationCommandValidator : AbstractValidator<GetRiqsTranslationCommand>
    {
        public GetRiqsTranslationCommandValidator()
        {
            RuleFor(command => command.PraxisClientId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("PraxisClientId can't be null.")
                .NotEmpty()
                .WithMessage("PraxisClientId can't be empty.");

            RuleFor(command => command.Texts)
                .NotEmpty().WithMessage("Texts list cannot be empty.")
                .Must(texts => texts.All(text => !string.IsNullOrWhiteSpace(text)))
                .WithMessage("All texts must be non-empty.");

            RuleFor(command => command.TranslateLangKeys)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("TranslateLangKeys can't be null.")
                .NotEmpty()
                .WithMessage("TranslateLangKeys can't be empty.");
        }

        public ValidationResult IsSatisfiedBy(GetRiqsTranslationCommand command)
        {
            var commandValidity = Validate(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

    }
}
