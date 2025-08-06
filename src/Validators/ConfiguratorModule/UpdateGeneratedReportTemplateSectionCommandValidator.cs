using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule;

public class UpdateGeneratedReportTemplateSectionCommandValidator : AbstractValidator<UpdateGeneratedReportTemplateSectionCommand>
{
    private readonly IRepository _repository;
    public UpdateGeneratedReportTemplateSectionCommandValidator(IRepository repository)
    {
        _repository = repository;
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(command => command)
            .NotNull()
            .WithMessage("Command can't be null");
        When(command => command != null, () =>
        {
            RuleFor(command => command.Sections)
                .NotEmpty()
                .WithMessage("At least one SectionElement is required");
            RuleForEach(command => command.Sections)
                .ChildRules(sectionElement =>
                {
                    sectionElement.RuleFor(command => command.ItemId)
                        .NotEmpty()
                        .WithMessage("ItemId can't be null or empty")
                        .Must(id => Guid.TryParse(id, out _))
                        .WithMessage("ItemId must be a valid GUID");
                    sectionElement.RuleFor(command => command.TemplateId)
                        .NotEmpty()
                        .WithMessage("TemplateId can't be null or empty")
                        .Must(id => Guid.TryParse(id, out _))
                        .WithMessage("TemplateId must be a valid GUID");
                        
                    sectionElement.RuleFor(command => command.SequenceNo)
                        .GreaterThan(0)
                        .WithMessage("SequenceNo must be greater than 0");
                    
                    sectionElement.RuleForEach(command => command.SectionElements)
                        .SetValidator(new GeneratedReportTemplateSectionElementValidator())
                        .When(command => command.SectionElements != null && command.SectionElements.Count > 0);
                });
            
            RuleFor(command => command)
                .MustAsync(async (command, ct) => await BeExistTemplateAsync(command, ct))
                .WithMessage("TemplateId Issue. Either TemplateId is not same for all sections or Template does not exist.");
                
        });
    }

    private async Task<bool> BeExistTemplateAsync(UpdateGeneratedReportTemplateSectionCommand command,
        CancellationToken ct)
    {
        var templateIds = command.Sections.Select(c => c.TemplateId).ToHashSet();
        if (templateIds.Count != 1) return false;
        return await _repository.ExistsAsync<PraxisGeneratedReportTemplateConfig>(pr => templateIds.Contains(pr.ItemId) && !pr.IsMarkedToDelete);;
    }
    
    public async Task<ValidationResult> IsSatisfiedBy(UpdateGeneratedReportTemplateSectionCommand command)
    {
        var commandValidity = await ValidateAsync(command);
        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
}