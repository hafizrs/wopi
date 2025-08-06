using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ConfiguratorModule
{
    public class CreateGeneratedReportTemplateSectionCommandValidationHandler : IValidationHandler<CreateGeneratedReportTemplateSectionCommand, CommandResponse>
    {
        private readonly CreateGeneratedReportTemplateSectionValidator _validator;
        private readonly IRepository _repository;
        public CreateGeneratedReportTemplateSectionCommandValidationHandler(
            CreateGeneratedReportTemplateSectionValidator validator,
            IRepository repository)
        {
            _validator = validator;
            _repository = repository;
        }
        public CommandResponse Validate(CreateGeneratedReportTemplateSectionCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(CreateGeneratedReportTemplateSectionCommand command)
        {
            var templateId = command.Sections?.FirstOrDefault()?.TemplateId;
            if (await IsDraftedReport(templateId)) return new CommandResponse();
            
            var validationResult = _validator.IsSatisfiedBy(command);
            return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
        }

        private async Task<bool> IsDraftedReport(string templateId)
        {
            var draftedReport = await _repository.GetItemAsync<PraxisGeneratedReportTemplateConfig>(x => x.ItemId == templateId && !x.IsMarkedToDelete);
            return draftedReport.Status == ReportStatus.Drafted;
        }
    }
}
