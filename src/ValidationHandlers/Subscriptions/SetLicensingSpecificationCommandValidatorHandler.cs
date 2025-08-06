using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    class SetLicensingSpecificationCommandValidatorHandler : IValidationHandler<SetLicensingSpecificationCommand, CommandResponse>
    {
        private readonly SetLicensingSpecificationCommandValidator _validationRules;

        public SetLicensingSpecificationCommandValidatorHandler(SetLicensingSpecificationCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }

        public CommandResponse Validate(SetLicensingSpecificationCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(SetLicensingSpecificationCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return validationResult.IsValid ? Task.FromResult(new CommandResponse()) : Task.FromResult(new CommandResponse(validationResult));
        }
    }
}
