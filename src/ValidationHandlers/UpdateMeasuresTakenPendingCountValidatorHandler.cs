using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateMeasuresTakenPendingCountValidatorHandler : IValidationHandler<UpdateMeasuresTakenPendingCountCommand, CommandResponse>
    {
        private readonly UpdateMeasuresTakenPendingCountValidator _validations;

        public UpdateMeasuresTakenPendingCountValidatorHandler(
            UpdateMeasuresTakenPendingCountValidator validations)
        {
            _validations = validations;
        }

        [Invocable]
        public CommandResponse Validate(UpdateMeasuresTakenPendingCountCommand command)
        {
            var validationResult = _validations.IsSatisfiedby(command);

            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }

        public Task<CommandResponse> ValidateAsync(UpdateMeasuresTakenPendingCountCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
