using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
   public class ProcessUserCreateUpdateValidationHandler : IValidationHandler<ProcessUserCreateUpdateCommand, CommandResponse>
    {
        private readonly ProcessUserCreateUpdateValidator validator;
        public ProcessUserCreateUpdateValidationHandler(ProcessUserCreateUpdateValidator validator)
        {
            this.validator = validator;
        }
        public CommandResponse Validate(ProcessUserCreateUpdateCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ProcessUserCreateUpdateCommand command)
        {
            var validationResult = validator.IsSatisfiedby(command);
            if (!validationResult.IsValid)
            {
                return Task.FromResult(new CommandResponse(validationResult));
            }
            return Task.FromResult(new CommandResponse());
            
        }
    }
}
