using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateShiftPlanCommandValidationHandler : IValidationHandler<UpdateShiftPlanCommand, CommandResponse>
    {
        private readonly UpdateShiftPlanCommandValidator _validationRules;

        public UpdateShiftPlanCommandValidationHandler(
            UpdateShiftPlanCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(UpdateShiftPlanCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdateShiftPlanCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
