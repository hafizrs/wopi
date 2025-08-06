using Selise.Ecap.SC.PraxisMonitor.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ConfiguratorModule
{
    public class CreateReportTemplateCommandValidationHandler : IValidationHandler<CreateReportTemplateCommand, CommandResponse>
    {
        private readonly CreateReportTemplateCommandValidator _validator;
        public CreateReportTemplateCommandValidationHandler(CreateReportTemplateCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(CreateReportTemplateCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(CreateReportTemplateCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);
            return Task.FromResult(!validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse());
        }
    }
}
