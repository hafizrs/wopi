using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.AbsenceModule
{
    public class DeleteAbsenceTypeCommandValidationHandler : IValidationHandler<DeleteAbsenceTypeCommand, CommandResponse>
    {
        private readonly DeleteAbsenceTypeCommandValidator _validator;

        public DeleteAbsenceTypeCommandValidationHandler(DeleteAbsenceTypeCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(DeleteAbsenceTypeCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(DeleteAbsenceTypeCommand command)
        {
            var validationResult = await _validator.IsSatisfiedby(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }
    }
}