using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class UpdateReportTemplateSectionCommandValidator : AbstractValidator<UpdateReportTemplateSectionCommand>
    {
        private readonly IRepository _repository;
        public UpdateReportTemplateSectionCommandValidator(IRepository repository)
        {
            _repository = repository;
            
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command can't be null");
            RuleForEach(command => command.Sections)
                .SetValidator(new PraxisReportTemplateSectionValidator(_repository));
            RuleFor(command => command)
                .MustAsync(async (command, cancellationToken) => await SameTemplateForAllSectionsExist(command, cancellationToken))
                .WithMessage("TemplateId Issue. Either TemplateId is not same for all sections or Template does not exist.");
            
        }

        private async Task<bool> SameTemplateForAllSectionsExist(UpdateReportTemplateSectionCommand command, CancellationToken cancellationToken)
        {
            var templateIds = command.Sections.Select(s => s.TemplateId).ToHashSet();
            if (templateIds.Count > 1) return false;
            return await _repository.ExistsAsync<PraxisReportTemplate>(pr => templateIds.Contains(pr.ItemId));
        }

        public async Task<ValidationResult> IsSatisfiedBy(UpdateReportTemplateSectionCommand command)
        {
            var validationResult = await ValidateAsync(command);
            return !validationResult.IsValid ? validationResult : new ValidationResult();
        }
    }
}
