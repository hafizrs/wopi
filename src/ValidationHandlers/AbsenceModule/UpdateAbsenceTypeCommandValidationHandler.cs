using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.AbsenceModule
{
    public class UpdateAbsenceTypeCommandValidationHandler : IValidationHandler<UpdateAbsenceTypeCommand, CommandResponse>
    {
        private readonly UpdateAbsenceTypeCommandValidator _validator;

        public UpdateAbsenceTypeCommandValidationHandler(UpdateAbsenceTypeCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(UpdateAbsenceTypeCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(UpdateAbsenceTypeCommand command)
        {
            var validationResult = await _validator.IsSatisfiedby(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }
    }
}