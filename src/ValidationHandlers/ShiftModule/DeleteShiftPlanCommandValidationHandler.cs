using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class DeleteShiftPlanCommandValidationHandler : IValidationHandler<DeleteShiftPlanCommand, CommandResponse>
    {
        private readonly DeleteShiftPlanCommandValidator _validationRules;

        public DeleteShiftPlanCommandValidationHandler(
            DeleteShiftPlanCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(DeleteShiftPlanCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(DeleteShiftPlanCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
