using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class SubscriptionUpdateForClientCommandValidatorHandler : IValidationHandler<SubscriptionUpdateForClientCommand, CommandResponse>
    {
        private readonly SubscriptionUpdateForClientCommandValidator _validator;

        public SubscriptionUpdateForClientCommandValidatorHandler(
            SubscriptionUpdateForClientCommandValidator validator)
        {
            _validator = validator;
        }


        public CommandResponse Validate(SubscriptionUpdateForClientCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(SubscriptionUpdateForClientCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return Task.FromResult(new CommandResponse(validationResult));
            return Task.FromResult(new CommandResponse());
        }
    }
}