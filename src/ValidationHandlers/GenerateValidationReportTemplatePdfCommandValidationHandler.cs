using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class GenerateValidationReportTemplatePdfCommandValidationHandler : IValidationHandler<GenerateValidationReportTemplatePdfCommand, CommandResponse>
    {
        private readonly GenerateValidationReportTemplatePdfCommandValidator _validator;
        public GenerateValidationReportTemplatePdfCommandValidationHandler(GenerateValidationReportTemplatePdfCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(GenerateValidationReportTemplatePdfCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(GenerateValidationReportTemplatePdfCommand command)
        {
            var validationResult = _validator.Validate(command);
            return Task.FromResult(!validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse());
        }
    }
}
