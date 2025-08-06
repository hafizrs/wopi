using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;
namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class CreateShiftCommandValidationHandler : IValidationHandler<CreateShiftCommand, CommandResponse>
    {
        private readonly CreateShiftCommandValidator _validationRules;

        public CreateShiftCommandValidationHandler(
            CreateShiftCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(CreateShiftCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(CreateShiftCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
