using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateClientPaymentCommandValidatorHandler : IValidationHandler<UpdateClientPaymentCommand, CommandResponse>
    {
        private readonly UpdateClientPaymentCommandValidator _updateClientPaymentCommandValidator;

        public UpdateClientPaymentCommandValidatorHandler(
            UpdateClientPaymentCommandValidator updateClientPaymentCommandValidator
            )
        {
            _updateClientPaymentCommandValidator = updateClientPaymentCommandValidator;
        }
        public CommandResponse Validate(UpdateClientPaymentCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdateClientPaymentCommand command)
        {
            var validationResult = _updateClientPaymentCommandValidator.IsSatisfiedby(command);
            return validationResult.IsValid ? Task.FromResult(new CommandResponse()) : Task.FromResult(new CommandResponse(validationResult));
        }
    }
}
