using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.AbsenceModule
{
    public class UpdateAbsencePlanCommandValidationHandler : IValidationHandler<UpdateAbsencePlanCommand, CommandResponse>
    {
        private readonly UpdateAbsencePlanCommandValidator _validator;

        public UpdateAbsencePlanCommandValidationHandler(UpdateAbsencePlanCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(UpdateAbsencePlanCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(UpdateAbsencePlanCommand command)
        {
            var validationResult = await _validator.IsSatisfiedby(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }
    }
}