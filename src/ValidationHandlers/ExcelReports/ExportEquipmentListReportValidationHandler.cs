using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class ExportEquipmentListReportValidationHandler : IValidationHandler<ExportEquipmentListReportCommand, CommandResponse>
    {
        private readonly ExportEquipmentListReportCommandValidator _validator;
        private readonly IPraxisClientService _praxisClientService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;

        public ExportEquipmentListReportValidationHandler(
            ExportEquipmentListReportCommandValidator validator,
            IPraxisClientService praxisClientService,
            ISecurityHelperService securityHelperService,
            IRepository repository,
            ISecurityContextProvider securityContextProvider)
        {
            _validator = validator;
            _praxisClientService = praxisClientService;
            _securityHelperService = securityHelperService;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
        }

        public CommandResponse Validate(ExportEquipmentListReportCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(ExportEquipmentListReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return new CommandResponse(validationResult);

            var response = new CommandResponse();

            bool isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "Guid is not valid of ReportFileId");

            if (!string.IsNullOrEmpty(command.ClientId))
            {
                var client = await _praxisClientService.GetPraxisClient(command.ClientId);

                if (client == null)
                    response.SetError("Exception", "Client not found for given ClientId");
            }
            else
            {
                var isAdminAOrB = _securityHelperService.IsAAdminOrTaskConrtroller() || _securityHelperService.IsAAdminBUser();
                var isAssignedOrgAdmin = IsEquipmentAssignedOrgAdmin(command.OrganizationId);
                if (!isAdminAOrB && !isAssignedOrgAdmin)
                {
                    response.SetError("Exception", "User is not authorized to perform this action");
                }
            }

            return response;
        }

        private bool IsEquipmentAssignedOrgAdmin(string organizationId)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;

            var isOrgAdmin = _repository
                .GetItem<PraxisEquipmentRight>(p =>
                    !p.IsMarkedToDelete &&
                    p.IsOrganizationLevelRight == true &&
                    p.OrganizationId == organizationId &&
                    p.AssignedAdmins != null &&
                    p.AssignedAdmins.Any(u => u.UserId == userId));
            return isOrgAdmin != null;
        }
    }
}