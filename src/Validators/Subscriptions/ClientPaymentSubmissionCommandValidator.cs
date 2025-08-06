using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ClientPaymentSubmissionCommandValidator : AbstractValidator<ClientPaymentSubmissionCommand>
    {
        public ClientPaymentSubmissionCommandValidator()
        {
            RuleFor(command => command.NumberOfUser)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("NumberOfUser can't be null.")
                            .NotEmpty().WithMessage("NumberOfUser can't be empty.")
                            .Must(IsValidNumber).WithMessage("Invalid NumberOfUser.");

            RuleFor(command => command.DurationOfSubscription)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("DurationOfSubscription can't be null.")
                            .NotEmpty().WithMessage("DurationOfSubscription can't be empty.")
                            .Must(IsValidDurationOfSubscription).WithMessage("DurationOfSubscription must be greater than zero.");

            RuleFor(command => command.OrganizationType)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("OrganizationType can't be null.")
                            .NotEmpty().WithMessage("OrganizationType can't be empty.");

            RuleFor(command => command.SubscriptionPackageId)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("SubscriptionId can't be null.")
                            .NotEmpty().WithMessage("SubscriptionId can't be empty.");

            RuleFor(command => command.Location)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("Location can't be null.")
                            .NotEmpty().WithMessage("Location can't be empty.");

            RuleFor(command => command.PerUserCost)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("PerUserCost can't be null.")
                            .NotEmpty().WithMessage("PerUserCost can't be empty.");

            RuleFor(command => command.AverageCost)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("AverageCost can't be null.")
                            .NotEmpty().WithMessage("AverageCost can't be empty.");

            RuleFor(command => command.TaxDeduction)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("TaxDeduction can't be null.");

            RuleFor(command => command.GrandTotal)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("GrandTotal can't be null.")
                            .NotEmpty().WithMessage("GrandTotal can't be empty.");

            RuleFor(command => command.PaymentCurrency)
                            .Cascade(CascadeMode.StopOnFirstFailure)
                            .NotNull().WithMessage("PaymentCurrency can't be null.")
                            .NotEmpty().WithMessage("PaymentCurrency can't be empty.");

        }

        private bool IsValidDurationOfSubscription(int duration)
        {
            return duration >= 1;
        }

        private bool IsValidNumber(int number)
        {
            return number >= PraxisConstants.OrganizationMinimumUserLimit && 
                   number <= PraxisConstants.OrganizationMaximumUserLimit;
        }

        public ValidationResult IsSatisfiedby(ClientPaymentSubmissionCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
