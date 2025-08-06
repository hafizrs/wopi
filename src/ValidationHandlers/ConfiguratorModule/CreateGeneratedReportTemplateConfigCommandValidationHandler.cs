using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ConfiguratorModule
{
    public class CreateGeneratedReportTemplateConfigCommandValidationHandler : IValidationHandler<CreateGeneratedReportTemplateConfigCommand, CommandResponse>
    {
        private readonly CreateGeneratedReportTemplateConfigCommandValidator _validator;
        public CreateGeneratedReportTemplateConfigCommandValidationHandler(CreateGeneratedReportTemplateConfigCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(CreateGeneratedReportTemplateConfigCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> ValidateAsync(CreateGeneratedReportTemplateConfigCommand command)
        {
            var validationResult = await _validator.IsSatisfiedby(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }
    }
}
