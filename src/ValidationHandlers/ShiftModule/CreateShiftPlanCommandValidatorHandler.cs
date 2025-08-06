using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class CreateShiftPlanCommandValidatorHandler : IValidationHandler<CreateShiftPlanCommand, CommandResponse>
    {
        private readonly CreateShiftPlanCommandValidator _validationRules;

        public CreateShiftPlanCommandValidatorHandler(
            CreateShiftPlanCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(CreateShiftPlanCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(CreateShiftPlanCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
}
