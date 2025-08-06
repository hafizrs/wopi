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
    public class DeleteGeneratedReportTemplateConfigCommandValidationHandler : IValidationHandler<DeleteGeneratedReportTemplateConfigCommand, CommandResponse>
    {
        private readonly DeleteGeneratedReportTemplateConfigCommandValidator _validator;
        public DeleteGeneratedReportTemplateConfigCommandValidationHandler(DeleteGeneratedReportTemplateConfigCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(DeleteGeneratedReportTemplateConfigCommand command)
        {
            throw new NotImplementedException();
        }
        public Task<CommandResponse> ValidateAsync(DeleteGeneratedReportTemplateConfigCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
