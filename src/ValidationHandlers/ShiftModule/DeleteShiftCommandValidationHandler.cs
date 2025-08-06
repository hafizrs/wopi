using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class DeleteShiftCommandValidationHandler : IValidationHandler<DeleteShiftCommand, CommandResponse>
    {
        private readonly DeleteShiftCommandValidator _validationRules;

        public DeleteShiftCommandValidationHandler(
            DeleteShiftCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(DeleteShiftCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(DeleteShiftCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
