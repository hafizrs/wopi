using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateProcessGuideCompletionStatusValidationHandler :
        IValidationHandler<UpdateProcessGuideCompletionStatusCommand, CommandResponse>
    {
        private readonly UpdateProcessGuideCompletionStatusCommandValidator _validator;

        public UpdateProcessGuideCompletionStatusValidationHandler(
            UpdateProcessGuideCompletionStatusCommandValidator validator
        )
        {
            _validator = validator;
        }

        public CommandResponse Validate(UpdateProcessGuideCompletionStatusCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }

        public Task<CommandResponse> ValidateAsync(UpdateProcessGuideCompletionStatusCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}