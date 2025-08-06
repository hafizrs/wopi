using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class AssignProcessGuideForEquipmentCommandValidationHandler : IValidationHandler<AssignProcessGuideForEquipmentCommand, CommandResponse>
    {
        private readonly AssignProcessGuideForEquipmentCommandValidator _validator;

        public AssignProcessGuideForEquipmentCommandValidationHandler(AssignProcessGuideForEquipmentCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(AssignProcessGuideForEquipmentCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(AssignProcessGuideForEquipmentCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}