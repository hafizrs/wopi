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
    public class UpdateReportTemplateCommandValidationHandler : IValidationHandler<UpdateReportTemplateCommand, CommandResponse>
    {
        private readonly UpdateReportTemplateCommandValidator _validator;
        public UpdateReportTemplateCommandValidationHandler(UpdateReportTemplateCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(UpdateReportTemplateCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(UpdateReportTemplateCommand command)
        {
            var result = await _validator.IsSatisfiedBy(command);
            return !result.IsValid ? new CommandResponse(result) : new CommandResponse();
        }
    }
}
