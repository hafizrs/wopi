using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class CreateDocumentEditMappingRecordValidationHandler : IValidationHandler<CreateDocumentEditMappingRecordCommand, RiqsCommandResponse>
    {
        private readonly CreateDocumentEditMappingRecordCommandValidator _validator;

        public CreateDocumentEditMappingRecordValidationHandler(CreateDocumentEditMappingRecordCommandValidator validator)
        {
            _validator = validator;
        }

        public RiqsCommandResponse Validate(CreateDocumentEditMappingRecordCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<RiqsCommandResponse> ValidateAsync(CreateDocumentEditMappingRecordCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new RiqsCommandResponse() : new RiqsCommandResponse(validationResult));
        }
    }
}
