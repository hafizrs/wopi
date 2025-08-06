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
    public class DeleteReportTemplateCommandValidationHandler : IValidationHandler<DeleteReportTemplateCommand, CommandResponse>
    {
        private readonly DeleteReportTemplateCommandValidator _validator;
        public DeleteReportTemplateCommandValidationHandler(DeleteReportTemplateCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(DeleteReportTemplateCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(DeleteReportTemplateCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
