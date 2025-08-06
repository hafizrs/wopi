using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdateMeasuresTakenPendingCountValidator : AbstractValidator<UpdateMeasuresTakenPendingCountCommand>
    {
        public UpdateMeasuresTakenPendingCountValidator()
        {
            RuleFor(command => command.RiskId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("RiskId required.")
                .Must(IsValidGuid).WithMessage("RiskId is not a valid guid.");

            RuleFor(command => command.OfflineMeasuresTaken)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("OfflineMeasuresTaken required.")
                .Must(IsValidValue).WithMessage("OfflineMeasuresTaken value must be greater than zero(0).");
        }

        private bool IsValidGuid(string itemId)
        {
            bool isValidGuid = Guid.TryParse(itemId, out _);

            if (!isValidGuid)
            {
                return false;
            }

            return true;
        }
        private bool IsValidValue(int OfflineMeasuresTaken)
        {
            return OfflineMeasuresTaken > 0;
        }

        public ValidationResult IsSatisfiedby(UpdateMeasuresTakenPendingCountCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
