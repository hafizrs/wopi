using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
   public class SubscriptionRenewalCommandValidatorHandler : IValidationHandler<SubscriptionRenewalCommand, CommandResponse>
    {
        private readonly SubscriptionRenewalCommandValidator _validator;

        public SubscriptionRenewalCommandValidatorHandler(
            SubscriptionRenewalCommandValidator validator)
        {
            _validator = validator;
        }


        public CommandResponse Validate(SubscriptionRenewalCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(SubscriptionRenewalCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return Task.FromResult( new CommandResponse(validationResult));
            return Task.FromResult( new CommandResponse());
        }
    }
}
