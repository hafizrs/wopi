using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class AssignTemplateToEquipmentCommandValidationHandler : IValidationHandler<AssignTemplateToEquipmentCommand, CommandResponse>
    {
        private readonly AssignTemplateToEquipmentCommandValidator _validator;
        public AssignTemplateToEquipmentCommandValidationHandler(AssignTemplateToEquipmentCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(AssignTemplateToEquipmentCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(AssignTemplateToEquipmentCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
