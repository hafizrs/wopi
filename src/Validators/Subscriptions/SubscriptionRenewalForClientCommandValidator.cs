using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class SubscriptionRenewalForClientCommandValidator : AbstractValidator<SubscriptionRenewalForClientCommand>
    {
        public SubscriptionRenewalForClientCommandValidator()
        {
            RuleFor(command => command.TotalAdditionalTokenInMillion)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Number Of Token can't be null")
                .NotEmpty().WithMessage("Number Of Token can't be empty");
            RuleFor(command => command.TotalAdditionalTokenCost)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Amount Of Token can't be null")
                .NotEmpty().WithMessage("Amount Of Token can't be empty");
            RuleFor(command => command.TotalAdditionalStorageInGigaBites)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Storage can't be null")
                .NotEmpty().WithMessage("Storage can't be empty");
            RuleFor(command => command.TotalAdditionalStorageCost)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Amount Of Storage can't be null")
                .NotEmpty().WithMessage("Amount Of Storage can't be empty");
            RuleFor(command => command.ClientId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ClientId can't be null")
                .NotEmpty().WithMessage("ClientId can't be empty");
        }

        public ValidationResult IsSatisfiedby(SubscriptionRenewalForClientCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
