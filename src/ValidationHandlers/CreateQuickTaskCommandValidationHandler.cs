using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class CreateQuickTaskCommandValidationHandler : IValidationHandler<CreateQuickTaskCommand, CommandResponse>
    {
        private readonly CreateQuickTaskCommandValidator _validationRules;

        public CreateQuickTaskCommandValidationHandler(CreateQuickTaskCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(CreateQuickTaskCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(CreateQuickTaskCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
} 