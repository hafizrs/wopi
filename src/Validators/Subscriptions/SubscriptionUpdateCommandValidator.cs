using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class SubscriptionUpdateCommandValidator : AbstractValidator<SubscriptionUpdateCommand>
    {
        private readonly IRepository _repository;
        public SubscriptionUpdateCommandValidator(IRepository repository)
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

        private bool IsValidNumberOfUser(SubscriptionUpdateCommand command)
        {
            var subscriptionData = _repository.GetItem<PraxisClientSubscription>(pcs => pcs.OrganizationId == command.OrganizationId && pcs.IsActive && pcs.IsLatest);
            return subscriptionData != null &&
                   command.NumberOfUser >= subscriptionData.NumberOfUser && 
                   command.NumberOfUser <= PraxisConstants.OrganizationMaximumUserLimit;
        }

        public ValidationResult IsSatisfiedby(SubscriptionUpdateCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
