using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
   public class SubscriptionUpdateCommandValidatorHandler : IValidationHandler<SubscriptionUpdateCommand, CommandResponse>
    {
        private readonly SubscriptionUpdateCommandValidator _validator;

        public SubscriptionUpdateCommandValidatorHandler(
            SubscriptionUpdateCommandValidator validator)
        {
            _validator = validator;
        }


        public CommandResponse Validate(SubscriptionUpdateCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(SubscriptionUpdateCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return Task.FromResult( new CommandResponse(validationResult));
            return Task.FromResult( new CommandResponse());
        }
    }
}
