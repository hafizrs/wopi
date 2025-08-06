using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateCustomSubscriptionValidatorHandler : IValidationHandler<UpdateCustomSubscriptionCommand, CommandResponse>
    {
        private readonly UpdateCustomSubscriptionValidator _validationRules;
        public UpdateCustomSubscriptionValidatorHandler(UpdateCustomSubscriptionValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(UpdateCustomSubscriptionCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdateCustomSubscriptionCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return validationResult.IsValid ? Task.FromResult(new CommandResponse()) : Task.FromResult(new CommandResponse(validationResult));
        }
    }
}
