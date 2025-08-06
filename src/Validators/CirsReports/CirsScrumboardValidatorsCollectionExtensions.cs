using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Update;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

public static class CirsScrumboardValidatorsCollectionExtensions
{
    public static void AddCirsScrumboardCommandValidators(this IServiceCollection services)
    {
        services.AddSingleton<CreateAnotherMessageCommandValidator>();
        services.AddSingleton<CreateHintReportCommandValidator>();
        services.AddSingleton<CreateIdeaReportCommandValidator>();
        services.AddSingleton<CreateIncidentReportCommandValidator>();
        services.AddSingleton<CreateComplainReportCommandValidator>();
        services.AddSingleton<CreateFaultReportCommandValidator>();

        services.AddSingleton<UpdateAnotherMessageCommandValidator>();
        services.AddSingleton<UpdateHintReportCommandValidator>();
        services.AddSingleton<UpdateIdeaReportCommandValidator>();
        services.AddSingleton<UpdateIncidentReportCommandValidator>();
        services.AddSingleton<UpdateComplainReportCommandValidator>();
        services.AddSingleton<UpdateFaultReportCommandValidator>();

        services.AddSingleton<DeleteCirsReportsCommandValidator>();
        services.AddSingleton<ActiveInactiveCirsReportCommandValidator>();
        services.AddSingleton<MoveToOtherDashboardCommandValidator>();
    }
}
