using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class RemoveCustomSubscriptionValidatorHandler : IValidationHandler<RemoveCustomSubscriptionCommand, CommandResponse>
    {
        private readonly RemoveCustomSubscriptionValidator _validationRules;
        public RemoveCustomSubscriptionValidatorHandler(RemoveCustomSubscriptionValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(RemoveCustomSubscriptionCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(RemoveCustomSubscriptionCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
