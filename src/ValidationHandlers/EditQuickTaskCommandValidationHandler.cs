using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class EditQuickTaskCommandValidationHandler : IValidationHandler<EditQuickTaskCommand, CommandResponse>
    {
        private readonly EditQuickTaskCommandValidator _validationRules;

        public EditQuickTaskCommandValidationHandler(EditQuickTaskCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(EditQuickTaskCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(EditQuickTaskCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
} 