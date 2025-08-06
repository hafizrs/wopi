using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class DeleteQuickTaskCommandValidationHandler : IValidationHandler<DeleteQuickTaskCommand, CommandResponse>
    {
        private readonly DeleteQuickTaskCommandValidator _validationRules;

        public DeleteQuickTaskCommandValidationHandler(DeleteQuickTaskCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(DeleteQuickTaskCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(DeleteQuickTaskCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
} 