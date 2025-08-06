using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ClientPaymentSubmissionValidatorHandler : IValidationHandler<ClientPaymentSubmissionCommand, CommandResponse>
    {
        private readonly ClientPaymentSubmissionCommandValidator _validationRules;

        public ClientPaymentSubmissionValidatorHandler(
            ClientPaymentSubmissionCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(ClientPaymentSubmissionCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ClientPaymentSubmissionCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
