using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class CreateQuickTaskPlanCommandValidationHandler : IValidationHandler<CreateQuickTaskPlanCommand, CommandResponse>
    {
        private readonly CreateQuickTaskPlanCommandValidator _validationRules;

        public CreateQuickTaskPlanCommandValidationHandler(CreateQuickTaskPlanCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(CreateQuickTaskPlanCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(CreateQuickTaskPlanCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
} 