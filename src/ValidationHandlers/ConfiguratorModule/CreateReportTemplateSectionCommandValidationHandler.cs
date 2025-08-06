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
    public class CreateReportTemplateSectionCommandValidationHandler : IValidationHandler<CreateReportTemplateSectionCommand, CommandResponse>
    {
        private readonly CreateReportTemplateSectionCommandValidator _validator;
        public CreateReportTemplateSectionCommandValidationHandler(CreateReportTemplateSectionCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(CreateReportTemplateSectionCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> ValidateAsync(CreateReportTemplateSectionCommand command)
        {
            var validationResult = await _validator.IsSatisfiedBy(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }
    }
}
