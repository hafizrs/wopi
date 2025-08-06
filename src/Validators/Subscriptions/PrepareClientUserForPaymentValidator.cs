using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class PrepareClientUserForPaymentValidator:AbstractValidator<PrepareClientUserForPaymentCommand>
    {
        private readonly IRepository _repository;

        public PrepareClientUserForPaymentValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(command => command.PaymentDetailId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("PaymentDetailId can't be null.")
                .NotEmpty().WithMessage("PaymentDetailId can't be empty.")
                .Must(IsValidGuid).WithMessage("PaymentDetailId is not a valid guid.")
                .Must(IsProcessSubscription).WithMessage("PaymentDetailId is not valid.");

            RuleFor(command => command.OrganizationData)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationData can't be null.")
                .NotEmpty().WithMessage("OrganizationData can't be empty.");

            RuleFor(command => command.OrganizationData.ItemId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationId can't be null.")
                .NotEmpty().WithMessage("OrganizationId can't be empty.")
                .Must(IsValidGuid).WithMessage("OrganizationId is not a valid guid.");

            RuleFor(command => command.OrganizationData.ClientName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationName can't be null.")
                .NotEmpty().WithMessage("OrganizationName can't be empty.");

            RuleFor(command => command.OrganizationData.UserLimit)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationUserLimit can't be null.")
                .NotEmpty().WithMessage("OrganizationUserLimit can't be empty.");

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsValidUserLimit).WithMessage("Invalid User Limit");

            RuleFor(command => command.OrganizationData.Address)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationAddress can't be null.")
                .NotEmpty().WithMessage("OrganizationAddress can't be empty.");

            RuleFor(command => command.ClientInformation)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ClientInformation can't be null.")
                .NotEmpty().WithMessage("ClientInformation can't be empty.");

            RuleFor(command => command.ClientInformation.NavigationList)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("NavigationList can't be null.")
                .NotEmpty().WithMessage("NavigationList can't be empty.");

            RuleFor(command => command.ClientInformation.NavigationProcessType)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("NavigationProcessType can't be null.")
                .NotEmpty().WithMessage("NavigationProcessType can't be empty.");

            RuleFor(command => command.ClientInformation.ClientData)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ClientData can't be null.")
                .NotEmpty().WithMessage("ClientData can't be empty.");

            RuleFor(command => command.AdminInformation)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("AdminInformation can't be null.")
                .NotEmpty().WithMessage("AdminInformation can't be empty.");

            RuleFor(command => command.AdminInformation.PersonalInformation)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("AdminUserPersonalInformation can't be null.")
                .NotEmpty().WithMessage("AdminUserPersonalInformation can't be empty.");

            RuleFor(command => command.AdminInformation.PersonalInformation.ItemId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("AdminUserId can't be null.")
                .NotEmpty().WithMessage("AdminUserId can't be empty.")
                .Must(IsValidGuid).WithMessage("AdminUserId is not a valid guid.");

            RuleFor(command => command.AdminInformation.PersonalInformation.Email)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("AdminUserEmail can't be null.")
               .NotEmpty().WithMessage("AdminUserEmail can't be empty.");

            RuleFor(command => command.AdminInformation.PersonalInformation.FirstName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("AdminUserFirstName can't be null.")
               .NotEmpty().WithMessage("AdminUserFirstName can't be empty.");

            RuleFor(command => command.AdminInformation.PersonalInformation.LastName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("AdminUserLastName can't be null.")
               .NotEmpty().WithMessage("AdminUserLastName can't be empty.");

            RuleFor(command => command.AdminInformation.PersonalInformation.DateOfBirth)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("AdminUserDateOfBirth can't be null.")
               .NotEmpty().WithMessage("AdminUserDateOfBirth can't be empty.");

            RuleFor(command => command.AdminInformation.PersonalInformation.DisplayName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("AdminUserDisplayName can't be null.")
               .NotEmpty().WithMessage("AdminUserDisplayName can't be empty.");

            RuleFor(command => command.DeputyInformation)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("DeputyInformation can't be null.")
                .NotEmpty().WithMessage("DeputyInformation can't be empty.");

            RuleFor(command => command.DeputyInformation.PersonalInformation)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("DeputyUserPersonalInformation can't be null.")
                .NotEmpty().WithMessage("DeputyUserPersonalInformation can't be empty.");

            RuleFor(command => command.DeputyInformation.PersonalInformation.ItemId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("DeputyUserId can't be null.")
                .NotEmpty().WithMessage("DeputyUserId can't be empty.")
                .Must(IsValidGuid).WithMessage("DeputyUserId is not a valid guid.");

            RuleFor(command => command.DeputyInformation.PersonalInformation.Email)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("DeputyUserEmail can't be null.")
               .NotEmpty().WithMessage("DeputyUserEmail can't be empty.");

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsSameAdminEmail).WithMessage("Same email has been provided for the admin user.");

            RuleFor(command => command.DeputyInformation.PersonalInformation.FirstName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("DeputyUserFirstName can't be null.")
               .NotEmpty().WithMessage("DeputyUserFirstName can't be empty.");

            RuleFor(command => command.DeputyInformation.PersonalInformation.LastName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("DeputyUserLastName can't be null.")
               .NotEmpty().WithMessage("DeputyUserLastName can't be empty.");

            RuleFor(command => command.DeputyInformation.PersonalInformation.DateOfBirth)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("DeputyUserDateOfBirth can't be null.")
               .NotEmpty().WithMessage("DeputyUserDateOfBirth can't be empty.");

            RuleFor(command => command.DeputyInformation.PersonalInformation.DisplayName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("DeputyUserDisplayName can't be null.")
               .NotEmpty().WithMessage("DeputyUserDisplayName can't be empty.");

            RuleFor(command => command.BillingAddress)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("BillingAddress can't be null.")
                .NotEmpty().WithMessage("BillingAddress can't be empty.");

            RuleFor(command => command.ResponsiblePerson)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("BillingAddress can't be null.")
                .NotEmpty().WithMessage("BillingAddress can't be empty.");

            RuleFor(command => command.Context)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Context can't be null.")
                .NotEmpty().WithMessage("Context can't be empty.");

            RuleFor(command => command.ActionName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ActionName can't be null.")
                .NotEmpty().WithMessage("ActionName can't be empty.");

            RuleFor(command => command.NotificationSubscriptionId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("NotificationSubscriptionId can't be null.")
                .NotEmpty().WithMessage("NotificationSubscriptionId can't be empty.");
        }

        private bool IsValidUserLimit(PrepareClientUserForPaymentCommand command)
        {
            var subscriptionData = GetSubscriptionData(command.PaymentDetailId);
            return subscriptionData != null && 
                   subscriptionData.NumberOfUser == command.OrganizationData.UserLimit &&
                   subscriptionData.NumberOfUser >= PraxisConstants.OrganizationMinimumUserLimit &&
                   subscriptionData.NumberOfUser <= PraxisConstants.OrganizationMaximumUserLimit;
        }

        private bool IsSameAdminEmail(PrepareClientUserForPaymentCommand command)
        {
            if (command.AdminInformation.PersonalInformation.Email.Equals(command.DeputyInformation.PersonalInformation.Email))
            {
                return false;
            }

            return true;
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

        private bool IsProcessSubscription(string paymentDetailId)
        {
            var subscriptionData = GetSubscriptionData(paymentDetailId);
            return subscriptionData != null && string.IsNullOrWhiteSpace(subscriptionData.OrganizationId);
        }

        private PraxisClientSubscription GetSubscriptionData(string paymentDetailId)
        {
            return _repository.GetItem<PraxisClientSubscription>(x => x.PaymentHistoryId == paymentDetailId);
        }


        public ValidationResult IsSatisfiedby(PrepareClientUserForPaymentCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
