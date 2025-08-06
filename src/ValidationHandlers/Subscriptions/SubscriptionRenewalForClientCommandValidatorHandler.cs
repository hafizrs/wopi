using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class SubscriptionRenewalForClientCommandValidatorHandler : IValidationHandler<SubscriptionRenewalForClientCommand, CommandResponse>
    {
        private readonly SubscriptionRenewalForClientCommandValidator _validator;

        public SubscriptionRenewalForClientCommandValidatorHandler(
            SubscriptionRenewalForClientCommandValidator validator)
        {
            _validator = validator;
        }


        public CommandResponse Validate(SubscriptionRenewalForClientCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(SubscriptionRenewalForClientCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return Task.FromResult(new CommandResponse(validationResult));
            return Task.FromResult(new CommandResponse());
        }
    }
}