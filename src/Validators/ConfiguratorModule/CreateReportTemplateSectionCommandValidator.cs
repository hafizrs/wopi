using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class CreateReportTemplateSectionCommandValidator : AbstractValidator<CreateReportTemplateSectionCommand>
    {
        private readonly ISecurityHelperService _securityHelperService;

        public CreateReportTemplateSectionCommandValidator(
            ISecurityHelperService securityHelperService,
            IRepository repository)
        {
            _securityHelperService = securityHelperService;
            
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(command => command)
                .Must(HaveCreateFormPermission)
                .WithMessage("User doesn't have permission to create this form");
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command can't be null");

            When(command => command != null, () =>
            {
                RuleFor(command => command.Sections)
                    .NotEmpty()
                    .WithMessage("Sections can't be null or empty");
                When(command => command.Sections != null, () => 
                {
                    RuleForEach(command => command.Sections)
                    .SetValidator(new PraxisReportTemplateSectionValidator(repository));
                });
            });
            
        }
        private bool HaveCreateFormPermission(CreateReportTemplateSectionCommand command)
        {
            return !_securityHelperService.IsAMpaUser();
        }
        public async Task<ValidationResult> IsSatisfiedBy(CreateReportTemplateSectionCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}
