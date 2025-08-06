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
    public class CreateGeneratedReportTemplateConfigCommandValidator : AbstractValidator<CreateGeneratedReportTemplateConfigCommand>
    {
        private readonly IRepository _repository;
        public CreateGeneratedReportTemplateConfigCommandValidator(IRepository repository)
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command can't be null");
            When(command => command != null, () =>
            {
                RuleFor(command => command.ItemId)
                    .NotEmpty()
                    .WithMessage("ItemId can't be empty")
                    .Must(id => Guid.TryParse(id, out _))
                    .WithMessage("ItemId must be a valid GUID");
                RuleFor(command => command.Status)
                    .IsInEnum()
                    .WithMessage("Status must be a valid enum value");
                RuleFor(command => command.TemplateId)
                    .NotEmpty()
                    .WithMessage("TemplateId can't be null or empty")
                    .MustAsync(async (id, ct) => await BeExistAsync(id, ct))
                    .WithMessage("Report template must be exist");
                RuleFor(command => command.GeneratedBy)
                    .NotEmpty()
                    .WithMessage("GeneratedBy can't be null or empty");

                RuleFor(command => command.Title)
                    .NotEmpty()
                    .WithMessage("Title can't be null or empty");

                //RuleFor(command => command.Description)
                //    .NotEmpty()
                //    .WithMessage("Description can't be null or empty");
                
                RuleFor(command => command.ClientInfos)
                    .NotEmpty()
                    .WithMessage("At least one ClientId is required");

                RuleFor(command => command.OrganizationInfos)
                    .NotEmpty()
                    .WithMessage("At least one OrganizationId is required");
            });
            _repository = repository;
        }

        private async Task<bool> BeExistAsync(string templateId, CancellationToken ct)
        {
            return await _repository.ExistsAsync<PraxisReportTemplate>(t => t.ItemId == templateId && !t.IsMarkedToDelete);
        }

        public async Task<ValidationResult> IsSatisfiedby(CreateGeneratedReportTemplateConfigCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}
