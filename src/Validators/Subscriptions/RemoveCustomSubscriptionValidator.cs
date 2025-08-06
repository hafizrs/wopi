using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class RemoveCustomSubscriptionValidator : AbstractValidator<RemoveCustomSubscriptionCommand>
    {
        public RemoveCustomSubscriptionValidator()
        {
            RuleFor(command => command.ClientId)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("Client Id can't be null")
                            .NotEmpty().WithMessage("ClientId can't be empty");
            RuleFor(command => command.NumberOfUser)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("Number Of User can't be null")
                            .NotEmpty().WithMessage("Number Of User can't be empty")
                            .Must(IsValidNumber).WithMessage("NumberOfUser must be greater than zero(0).");
            RuleFor(command => command.DurationOfSubscription)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("DurationOfSubscription can't be null.")
                            .NotEmpty().WithMessage("DurationOfSubscription can't be empty.")
                            .Must(IsValidNumber).WithMessage("DurationOfSubscription must be greater than zero(0).");
            RuleFor(command => command.AdditionalStorage)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("Additional Storage can't be null");

        }
        private bool IsValidNumber(int number)
        {
            return number >= 1;
        }
        public ValidationResult IsSatisfiedby(RemoveCustomSubscriptionCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
