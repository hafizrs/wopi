
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public static class PraxisEquipmentModuleConstants
    {
        public static readonly string HtmlTemplateFileId = "fa9eef06-1a21-4f04-a7c9-e566ad772423";
        public static readonly List<string> ReportTemplatePdfTranslationKeys = new()
            {
                "APP_EQUIPMENT_MANAGEMENT.EQUIPMENT_DETAILS",
                "APP_EQUIPMENT_MANAGEMENT.EQUIPMENT_NAME",
                "APP_EQUIPMENT_MANAGEMENT.ORGANIZATION",
                "APP_EQUIPMENT_MANAGEMENT.ROOM",
                "APP_EQUIPMENT_MANAGEMENT.SUPPLIERS",
                "APP_EQUIPMENT_MANAGEMENT.SERIAL_NUMBER",
                "APP_EQUIPMENT_MANAGEMENT.INSTALLATION_NUMBER",
                "APP_EQUIPMENT_MANAGEMENT.UDI_NUMBER",
                "APP_EQUIPMENT_MANAGEMENT.INTERNAL_NUMBER",
                "APP_EQUIPMENT_MANAGEMENT.DATE_OF_PURCHASE",
                "APP_EQUIPMENT_MANAGEMENT.DATE_OF_PLACING_IN_SERVICE",
                "APP_EQUIPMENT_MANAGEMENT.MAINTENANCE",
                "APP_EQUIPMENT_MANAGEMENT.MANUFACTURER",
                "APP_EQUIPMENT_MANAGEMENT.LAST_MAINTENANCE",
                "APP_EQUIPMENT_MANAGEMENT.NEXT_MAINTENANCE",
                "APP_EQUIPMENT_MANAGEMENT.CONTACT_INFORMATION",
                "APP_EQUIPMENT_MANAGEMENT.COMPANY",
                "APP_EQUIPMENT_MANAGEMENT.CONTACT_PERSON",
                "APP_EQUIPMENT_MANAGEMENT.EMAIL",
                "APP_EQUIPMENT_MANAGEMENT.PHONE",
                "APP_EQUIPMENT_MANAGEMENT.CATEGORY",
                "APP_EQUIPMENT_MANAGEMENT.SUB_CATEGORY",
                "APP_EQUIPMENT_MANAGEMENT.TABLE_OF_CONTENT",

                "APP_REPORT_TEMPLATE.DEVIATION_NAME",
                "APP_REPORT_TEMPLATE.DEVIATION_ABBREVIATION",
                "APP_REPORT_TEMPLATE.DEVIATION_DESCRIPTION",
                "APP_REPORT_TEMPLATE.DESCRIPTION",
                "APP_REPORT_TEMPLATE.PROPOSED_CORRECTION",
                "APP_REPORT_TEMPLATE.EVENT_DESCRIPTION",
                "APP_REPORT_TEMPLATE.REMARKS",
                "APP_REPORT_TEMPLATE.CRITICAL",
                "APP_REPORT_TEMPLATE.NON_CRITICAL"
            };
    }
}
