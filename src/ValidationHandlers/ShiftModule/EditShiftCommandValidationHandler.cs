using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class EditShiftCommandValidationHandler : IValidationHandler<EditShiftCommand, CommandResponse>
    {
        private readonly EditShiftCommandValidator _validationRules;

        public EditShiftCommandValidationHandler(
            EditShiftCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(EditShiftCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(EditShiftCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
