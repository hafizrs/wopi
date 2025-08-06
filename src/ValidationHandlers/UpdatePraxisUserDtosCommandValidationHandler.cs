using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdatePraxisUserDtosCommandValidationHandler: IValidationHandler<UpdatePraxisUserDtosCommand, CommandResponse>
    {
        private readonly UpdatePraxisUserDtosCommandValidator _validator;

        public UpdatePraxisUserDtosCommandValidationHandler(UpdatePraxisUserDtosCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(UpdatePraxisUserDtosCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult);
        }

        public Task<CommandResponse> ValidateAsync(UpdatePraxisUserDtosCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}