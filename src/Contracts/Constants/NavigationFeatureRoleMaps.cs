using Selise.Ecap.Entities.PrimaryEntities.SWICA;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public static class NavigationFeatureRoleMaps
    {
        public const string RiskManagementNavigationFeatureId = "RiskManagement.Navigation";
        public const string ProcessMonitoringNavigationFeatureId = "ProcessMonitoring.Navigation";
        public const string PraxisFormNavigationFeatureId = "PraxisForm.Navigation";
        public const string ReportNavigationFeatureId = "Report.Navigation";
        public const string PraxisOpenItemFeatureId = "PraxisOpenItem.Navigation";
        public const string TrainingNavigationFeatureId = "Training.Navigation";
        public const string CockpitNavigationFeatureId = "Cockpit.Navigation";
        public const string PraxisIncidentReportNavigationFeatureId = "PraxisIncidentReport.Navigation";
        public const string EquipmentManagementNavigationFeatureId = "EquipmentManagement.Navigation";
        public const string ProcessGuideNavigationFeatureId = "ProcessGuide.Navigation";
        public const string PraxisNavigationEmployeeType = "StaticNavigation";
        public const string PraxisNavigationEmployeeName = "PraxisNavigationEmployee";
        public const string PraxisNavigationEmployeeFeatureIdShiftPlan = "Praxis.Navigation.Employee_Shift_Plan";
        public const string PraxisNavigationEmployeeFeatureIdTemplates = "Praxis.Navigation.Employee_Templates";
        public const string PraxisClientNavigationFeatureId = "PraxisClient.Navigation";


        public static readonly string[] InaccessibleNavigationsForMpaGroup1 = 
        {
            RiskManagementNavigationFeatureId,
            ProcessMonitoringNavigationFeatureId,
            PraxisFormNavigationFeatureId,
            ReportNavigationFeatureId
        };
        public static readonly string[] InaccessibleNavigationsForMpaGroup2 =
        {
            RiskManagementNavigationFeatureId,
            ProcessMonitoringNavigationFeatureId,
            PraxisFormNavigationFeatureId,
            ReportNavigationFeatureId
        };
    }
}