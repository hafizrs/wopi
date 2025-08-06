using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisEquipmentForReport
    {
        private string _dateOfPurchase;
        private string _dateOfPlacingInService;
        private string _lastMaintenanceDate;
        private string _nextMaintenanceDate;
        public string Department { get; set; }
        public string ClientId { get; set; }
        public string EquipmentName { get; set;}
        public string Location { get; set; }
        public string Suppliers { get; set; }
        public string Manufacturer { get; set; }

        public string DateOfPurchase
        {
            get => _dateOfPurchase;
            set => _dateOfPurchase = GetDateTimeString(value);
        }

        public string DateOfPlacingInService
        {
            get => _dateOfPlacingInService;
            set => _dateOfPlacingInService = GetDateTimeString(value);
        }
        public bool MaintenanceMode { get; set; }
        public string SerialNumber { get; set; }
        public string InternalNumber { get; set; }
        public string InstallationNumber { get; set; }
        public string UDINumber { get; set; }
        public string LocationAddress { get; set; }
        public string ExactLocation { get; set; }
        public string LastMaintenanceDate
        {
            get => _lastMaintenanceDate;
            set => _lastMaintenanceDate = GetDateTimeString(value);
        }
        public string NextMaintenanceDate
        {
            get => _nextMaintenanceDate;
            set => _nextMaintenanceDate = GetDateTimeString(value);
        }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public IEnumerable<PraxisEquipmentAdditionalInfo> AdditionalInformation { get; set; }
        public PraxisEquipmentContactInformation ContactInformation { get; set; }
        public LocationChangeLog LocationLog { get; set; }
        public IEnumerable<PraxisImage> Photos { get; set; }
        public IEnumerable<PraxisDocument> Files { get; set; }
        public IEnumerable<PraxisAdditionalInfoTitleWithUser> UserAdditionalInformation { get; set; }

        private string GetDateTimeString(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;
            if (DateTime.TryParse(dateString, out var date))
            {
                return date.Year < 2000 ? null : date.ToString("MM/dd/yyyy");
            }
            return null;
        }
    }

    public class PraxisEquipmentContactInformation
    {
        public string ContactPerson { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class PraxisAdditionalInfoTitleWithUser
    {
        public string Title { get; set; }
        public List<string> UserList { get; set; }
    }
}