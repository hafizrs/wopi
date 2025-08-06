using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class PraxisReportTemplateSectionValidator : AbstractValidator<PraxisReportTemplateSection>
    {
        private readonly IRepository _repository;
        public PraxisReportTemplateSectionValidator(IRepository repository)
        {
            RuleFor(command => command.ItemId)
                    .NotEmpty()
                    .WithMessage("ItemId can't be null or empty")
                    .Must(id => Guid.TryParse(id, out _))
                    .WithMessage("ItemId must be a valid GUID");
            RuleFor(command => command.TemplateId)
                    .NotEmpty()
                    .WithMessage("TemplateId can't be null or empty")
                    .Must(id => Guid.TryParse(id, out _))
                    .WithMessage("TemplateId must be a valid GUID")
                    .MustAsync(async (id, ct) => await BeExistAsync(id, ct))
                    .WithMessage("Report template must be exist");
            RuleFor(command => command.SequenceNo)
                .GreaterThan(0)
                .WithMessage("SequenceNo must be greater than 0");
            
            RuleForEach(command => command.SectionElements)
                .ChildRules(CommonRuleValidator.ApplyCommonReportTemplateSectionElementRule)
                .When(command => command.SectionElements != null && command.SectionElements.Any());
            _repository = repository;
        }
        
        private async Task<bool> BeExistAsync(string templateId, CancellationToken ct)
        {
            return await _repository.ExistsAsync<PraxisReportTemplate>(t => t.ItemId == templateId && !t.IsMarkedToDelete);
        }
    }
}
