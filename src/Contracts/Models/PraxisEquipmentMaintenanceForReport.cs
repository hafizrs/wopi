using System;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisEquipmentMaintenanceForReport
    {
        private string _maintenanceStartDate;
        private string _maintenanceEndDate;
        public string EquipmentName { get; set; }
        public string Department { get;  set; }
        public List<string> ExecutingGroup { get; set; }
        public string ProcessGuide { get; set; }
        public List<string> CompletedBy { get; set; }
        public int MaintenancePeriod { get; set; }
        public string ScheduleType { get; set; }
        public string Remarks { get; set; }
        public List<string> Supplier { get; set; }

        public string MaintenanceStartDate
        {
            get => _maintenanceStartDate;
            set => _maintenanceStartDate = GetDateTimeString(value);
        }
        public string MaintenanceEndDate
        {
            get => _maintenanceEndDate;
            set => _maintenanceEndDate = GetDateTimeString(value, 1000);
        }

        public List<string> Approver { get; set; }
        public string Status { get; set; }
        public List<string> Library { get; set; }
        public List<string> Pending { get; set; }
        public List<PraxisEquipmentMaintenanceResponse> Responses { get; set; }
        public List<PraxisEquipmentMaintenanceResponse> SupplierResponses { get; set; }

        private string GetDateTimeString(string dateString, int year = 2000)
        {
            try
            {
                var date = DateTime.Parse(dateString);
                return (date.Year < year) ? null : date.ToString("dd.MM.yyyy");
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
    public class PraxisEquipmentMaintenanceResponse : PraxisEquipmentMaintenanceResponseBase
    {
        public PraxisEquipmentMaintenanceResponseBase ApprovalResponse { get; set; }
    }
    public class PraxisEquipmentMaintenanceResponseBase : EquipmentMaintenanceAnswerBase
    {
        public string ReportedByName { get; set; }
    }

    
}