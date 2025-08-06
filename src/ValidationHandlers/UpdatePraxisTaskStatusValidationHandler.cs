using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdatePraxisTaskStatusValidationHandler : IValidationHandler<UpdatePraxisTaskStatusCommand, CommandResponse>
    {
        private readonly UpdatePraxisTaskStatusCommandValidator validator;

        public UpdatePraxisTaskStatusValidationHandler(
            UpdatePraxisTaskStatusCommandValidator validator
        )
        {
            this.validator = validator;
        }

        public CommandResponse Validate(UpdatePraxisTaskStatusCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdatePraxisTaskStatusCommand command)
        {
            var validationResult = validator.IsSatisfiedby(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}