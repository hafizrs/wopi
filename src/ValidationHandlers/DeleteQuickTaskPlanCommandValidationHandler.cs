using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class DeleteQuickTaskPlanCommandValidationHandler : IValidationHandler<DeleteQuickTaskPlanCommand, CommandResponse>
    {
        private readonly DeleteQuickTaskPlanCommandValidator _validationRules;

        public DeleteQuickTaskPlanCommandValidationHandler(DeleteQuickTaskPlanCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(DeleteQuickTaskPlanCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(DeleteQuickTaskPlanCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
} 