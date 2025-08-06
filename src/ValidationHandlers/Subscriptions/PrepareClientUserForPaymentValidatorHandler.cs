using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
   public class PrepareClientUserForPaymentValidatorHandler : IValidationHandler<PrepareClientUserForPaymentCommand, CommandResponse>
    {
        private readonly PrepareClientUserForPaymentValidator _validator;

        public PrepareClientUserForPaymentValidatorHandler(
            PrepareClientUserForPaymentValidator validator)
        {
            _validator = validator;
        }


        public CommandResponse Validate(PrepareClientUserForPaymentCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(PrepareClientUserForPaymentCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return Task.FromResult( new CommandResponse(validationResult));
            return Task.FromResult( new CommandResponse());
        }
    }
}
