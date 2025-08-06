using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.AbsenceModule
{
    public class DeleteAbsencePlanCommandValidationHandler : IValidationHandler<DeleteAbsencePlanCommand, CommandResponse>
    {
        private readonly DeleteAbsencePlanCommandValidator _validator;

        public DeleteAbsencePlanCommandValidationHandler(DeleteAbsencePlanCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(DeleteAbsencePlanCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(DeleteAbsencePlanCommand command)
        {
            var validationResult = await _validator.IsSatisfiedby(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }
    }
}