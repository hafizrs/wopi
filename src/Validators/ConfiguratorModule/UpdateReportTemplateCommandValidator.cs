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
    public class UpdateReportTemplateCommandValidator : AbstractValidator<UpdateReportTemplateCommand>
    {
        private readonly IRepository _repository;
        public UpdateReportTemplateCommandValidator(IRepository repository) 
        {
            RuleFor(x => x.ItemId)
                .NotEmpty()
                .WithMessage("ItemId is required.")
                .MustAsync(async (id, cancellation) => await ExistTemplateAsync(id, cancellation))
                .WithMessage("Template with this ItemId does not exist.");
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.");

            RuleFor(x => x.ClientInfos)
                .NotEmpty()
                .WithMessage("ClientInfos are required.");
            RuleFor(x => x.OrganizationInfos)
                .NotEmpty()
                .WithMessage("OrganizationInfos are required.");
            //RuleFor(x => x.HeaderText)
            //    .NotEmpty()
            //    .WithMessage("HeaderText is required.");
            //RuleFor(x => x.FooterText)
            //    .NotEmpty()
            //    .WithMessage("FooterText is required.");

            _repository = repository;
        }

        private async Task<bool> ExistTemplateAsync(string itemId, CancellationToken cancellation)
        {
            return await _repository.ExistsAsync<PraxisReportTemplate>(x => x.ItemId == itemId);
        }

        public async Task<ValidationResult> IsSatisfiedBy(UpdateReportTemplateCommand command)
        {
            var validationResult = await ValidateAsync(command);
            return !validationResult.IsValid ? validationResult : new ValidationResult();
        }
    }
}
