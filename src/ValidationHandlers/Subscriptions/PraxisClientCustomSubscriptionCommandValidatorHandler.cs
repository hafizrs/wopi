using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class PraxisClientCustomSubscriptionCommandValidatorHandler : IValidationHandler<PraxisClientCustomSubscriptionCommand, CommandResponse>
    {
        public readonly PraxisClientCustomSubscriptionCommandValidator _validationRules;
        public PraxisClientCustomSubscriptionCommandValidatorHandler(PraxisClientCustomSubscriptionCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(PraxisClientCustomSubscriptionCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(PraxisClientCustomSubscriptionCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
