using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule;

public class UpdateGeneratedReportTemplateConfigCommandValidator : AbstractValidator<UpdateGeneratedReportTemplateConfigCommand>
{
    private readonly IRepository _repository;

    public UpdateGeneratedReportTemplateConfigCommandValidator(IRepository repository)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        
        RuleFor(command => command)
            .NotNull()
            .WithMessage("Command can't be null");
        RuleFor(command => command.Title)
            .NotEmpty()
            .WithMessage("Title can't be empty");
        RuleFor(command => command.ItemId)
            .NotEmpty()
            .WithMessage("ItemId can't be empty")
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage("ItemId must be a valid GUID")
            .MustAsync(async (id, ct) => await BeExistReportTemplateConfig(id, ct))
            .WithMessage("Report template config must be exist");
        RuleFor(command => command.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid enum value");
        //RuleFor(command => command.HeaderText)
        //    .NotEmpty()
        //    .WithMessage("HeaderText can't be empty");
        //RuleFor(command => command.FooterText)
        //    .NotEmpty()
        //    .WithMessage("FooterText can't be empty");
        
        _repository = repository;
    }

    private async Task<bool> BeExistReportTemplateConfig(string itemId, CancellationToken ct)
    {
        return await _repository.ExistsAsync<PraxisGeneratedReportTemplateConfig>(gr => gr.ItemId == itemId && !gr.IsMarkedToDelete);
    }

    public async Task<ValidationResult> IsSatisfiedby(UpdateGeneratedReportTemplateConfigCommand command)
    {
        var commandValidity = await ValidateAsync(command);
        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
}