using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class SubscriptionRenewalCommandValidator : AbstractValidator<SubscriptionRenewalCommand>
    {
        private readonly IRepository _repository;
        public SubscriptionRenewalCommandValidator(IRepository repository)
        {
            _repository = repository;
            RuleFor(command => command.NumberOfUser)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Number Of User can't be null")
                .NotEmpty().WithMessage("Number Of User can't be empty");
            RuleFor(command => command.OrganizationId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationId can't be null")
                .NotEmpty().WithMessage("OrganizationId can't be empty");
            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsValidNumberOfUser).WithMessage("Invalid NumberOfUser.");


        }
        private bool IsValidNumberOfUser(SubscriptionRenewalCommand command)
        {
            var orgData = _repository.GetItem<PraxisOrganization>(o => o.ItemId == command.OrganizationId && !o.IsMarkedToDelete);
            if (orgData != null)
            {
                var minNumberOfUser = Math.Max(PraxisConstants.OrganizationMinimumUserLimit, orgData.TotalDepartmentUserLimit);
                return command.NumberOfUser >= minNumberOfUser &&
                       command.NumberOfUser <= PraxisConstants.OrganizationMaximumUserLimit;
            }
            return false;
        }
        public ValidationResult IsSatisfiedby(SubscriptionRenewalCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
