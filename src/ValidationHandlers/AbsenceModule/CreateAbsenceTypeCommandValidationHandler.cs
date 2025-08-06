using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.AbsenceModule
{
    public class CreateAbsenceTypeCommandValidationHandler : IValidationHandler<CreateAbsenceTypeCommand, CommandResponse>
    {
        private readonly CreateAbsenceTypeCommandValidator _validator;
        public CreateAbsenceTypeCommandValidationHandler(CreateAbsenceTypeCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(CreateAbsenceTypeCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(CreateAbsenceTypeCommand command)
        {
            var validationResult = await _validator.IsSatisfiedby(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }
    }
}