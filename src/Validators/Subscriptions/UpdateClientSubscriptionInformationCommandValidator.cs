using FluentValidation;
using FluentValidation.Results;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class UpdateClientSubscriptionInformationCommandValidator : AbstractValidator<UpdateClientSubscriptionInformationCommand>
    {
        public UpdateClientSubscriptionInformationCommandValidator()
        {
            //RuleFor(command => command.OrganizationId)
            //    .Cascade(CascadeMode.StopOnFirstFailure)
            //    .NotNull().WithMessage("OrganizationId can't be null.")
            //    .NotEmpty().WithMessage("OrganizationId can't be empty.")
            //    .Must(IsValidGuid).WithMessage("OrganizationId is not valid.");

            RuleFor(command => command.PaymentDetailId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("PaymentDetailId can't be null.")
                .NotEmpty().WithMessage("PaymentDetailId can't be empty.")
                .Must(IsValidGuid).WithMessage("PaymentDetailId is not valid.");

            RuleFor(command => command.ActionName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ActionName can't be null.")
                .NotEmpty().WithMessage("ActionName can't be empty.");

            RuleFor(command => command.Context)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Context can't be null.")
                .NotEmpty().WithMessage("Context can't be empty.");

            RuleFor(command => command.NotificationSubscriptionId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("NotificationSubscriptionId can't be null.")
                .NotEmpty().WithMessage("NotificationSubscriptionId can't be empty.")
                .Must(IsValidGuid).WithMessage("NotificationSubscriptionId is not valid.");
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

        public ValidationResult IsSatisfiedby(UpdateClientSubscriptionInformationCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
