using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateClientSubscriptionInformationCommandValidatorHandler : IValidationHandler<UpdateClientSubscriptionInformationCommand, CommandResponse>
    {
        private readonly UpdateClientSubscriptionInformationCommandValidator _updateClientSubscriptionInfoCommandValidator;

        public UpdateClientSubscriptionInformationCommandValidatorHandler(
            UpdateClientSubscriptionInformationCommandValidator updateClientSubscriptionInfoCommandValidator)
        {
            _updateClientSubscriptionInfoCommandValidator = updateClientSubscriptionInfoCommandValidator;
        }

        public CommandResponse Validate(UpdateClientSubscriptionInformationCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdateClientSubscriptionInformationCommand command)
        {
            var validationResult = _updateClientSubscriptionInfoCommandValidator.IsSatisfiedby(command);
            return validationResult.IsValid ? Task.FromResult(new CommandResponse()) : Task.FromResult(new CommandResponse(validationResult));
        }
    }
}
