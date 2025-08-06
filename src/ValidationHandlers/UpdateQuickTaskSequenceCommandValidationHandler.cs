using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateQuickTaskSequenceCommandValidationHandler : IValidationHandler<UpdateQuickTaskSequenceCommand, CommandResponse>
    {
        private readonly UpdateQuickTaskSequenceCommandValidator _validationRules;

        public UpdateQuickTaskSequenceCommandValidationHandler(
            UpdateQuickTaskSequenceCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(UpdateQuickTaskSequenceCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdateQuickTaskSequenceCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return !validationResult.IsValid ? Task.FromResult(new CommandResponse(validationResult)) : Task.FromResult(new CommandResponse());
        }
    }
} 