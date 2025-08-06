
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports;

public class ExportLibraryDocumentAssigneeReportValidationHandler :
    IValidationHandler<ExportLibraryDocumentAssigneesReportCommand, CommandResponse>
{
    private readonly ExportLibraryDocumentAssigneeReportValidator _validator;

    public ExportLibraryDocumentAssigneeReportValidationHandler(
        ExportLibraryDocumentAssigneeReportValidator validator)
    {
        _validator = validator;
    }
    public CommandResponse Validate(ExportLibraryDocumentAssigneesReportCommand command)
    {
        throw new System.NotImplementedException();
    }

    public Task<CommandResponse> ValidateAsync(ExportLibraryDocumentAssigneesReportCommand command)
    {
        var validationResult = _validator.IsSatisfiedBy(command);

        var response = new CommandResponse();
        if (!validationResult.IsValid)
            response = new CommandResponse(validationResult);
        return Task.FromResult(response);
    }
}