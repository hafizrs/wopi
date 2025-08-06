using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class AssignEquipmentAdminsCommandValidationHandler : IValidationHandler<AssignEquipmentAdminsCommand,
        CommandResponse>
    {
        private readonly AssignEquipmentAdminsCommandValidator _validator;

        public AssignEquipmentAdminsCommandValidationHandler(AssignEquipmentAdminsCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(AssignEquipmentAdminsCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(AssignEquipmentAdminsCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}