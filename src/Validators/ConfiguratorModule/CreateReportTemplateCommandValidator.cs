using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class CreateReportTemplateCommandValidator : AbstractValidator<CreateReportTemplateCommand>
    {
        private readonly ISecurityHelperService _securityHelperService;
        public CreateReportTemplateCommandValidator(ISecurityHelperService securityHelperService)
        {
            // Set RuleLevelCascadeMode explicitly to replace StopOnFirstFailure
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(command => command)
                .Must(HaveCreateFormPermission)
                .WithMessage("User doesn't have permission to create this form");

            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command can't be null");


            When(command => command != null, () =>
            {
                RuleFor(command => command.ItemId)
                    .NotEmpty()
                    .WithMessage("ItemId can't be null or empty")
                    .Must(id => Guid.TryParse(id, out _))
                    .WithMessage("ItemId must be a valid GUID");

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

                //RuleFor(command => command.HeaderText)
                //    .NotEmpty()
                //    .WithMessage("HeaderText can't be null or empty");

                //RuleFor(command => command.FooterText)
                //    .NotEmpty()
                //    .WithMessage("FooterText can't be null or empty");

                //RuleFor(command => command.Logo)
                //    .NotEmpty()
                //    .WithMessage("Logo can't be null or empty");

                RuleFor(command => command.Status)
                    .IsInEnum()
                    .WithMessage("Status must be a valid enum value");
            });

            _securityHelperService = securityHelperService;

        }

        private bool HaveCreateFormPermission(CreateReportTemplateCommand command)
        {
            return !_securityHelperService.IsAMpaUser();
        }

        public ValidationResult IsSatisfiedby(CreateReportTemplateCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
}
