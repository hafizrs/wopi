using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public static class ReportConstants
    {
        public static List<string> ImageExts { get; set; } = new List<string>
        {
            "jpg", "jpeg", "png", "ico", "x-icon", "gif"
        };
        public static List<string> TopicTranslationKeys { get; set; } = new List<string>
        {
            "DRUG_PRODUCT",
            "APPLIANCES",
            "HYGIENE",
            "PROCESSES",
            "STAFF",
            "INSTRUMENTS",
            "DIAGNOSTIC",
            "INFRASTRUCTURE",
            "DOCUMENTATION",
            "ADMINISTRATION"
        };

        public static List<string> FormPurposeTranslationKeys { get; set; } = new List<string>
        {
            "TASK_LIST",
            "TRAINING_MODULE",
            "PROCESS_GUIDE"
        };

        public static List<string> ScheduleRecurrenceTranslationKeys { get; set; } = new List<string>
        {
            "DAILY",
            "WEEKLY",
            "MONTHLY",
            "YEARLY"
        };

        public static List<string> WeekDaysTranslationKeys { get; set; } = new List<string>
        {
            "MONDAY",
            "TUESDAY",
            "WEDNESDAY",
            "THURSDAY",
            "FRIDAY",
            "SATURDAY",
            "SUNDAY"
        };

        public static List<string> BooleanTranslationKeys { get; set; } = new List<string>
        {
            "YES",
            "NO"
        };

        public static List<string> RepeatEveryTranslationKeys { get; set; } = new List<string>
        {
            "1_DAY",
            "2_DAYS",
            "3_DAYS",
            "4_DAYS",
            "5_DAYS",
            "6_DAYS",
            "7_DAYS",
            "8_DAYS",
            "9_DAYS",
            "10_DAYS",
            "11_DAYS",
            "12_DAYS",
            "13_DAYS",
            "14_DAYS",
            "15_DAYS",
            "16_DAYS",
            "17_DAYS",
            "18_DAYS",
            "19_DAYS",
            "20_DAYS",
            "21_DAYS",
            "22_DAYS",
            "23_DAYS",
            "24_DAYS",
            "25_DAYS",
            "26_DAYS",
            "27_DAYS",
            "28_DAYS",
            "29_DAYS",
            "30_DAYS"
        };

        public static List<string> RepeatEveryMonthlyTranslationKeys { get; set; } = new List<string>
        {
            "1_MONTH",
            "2_MONTHS",
            "3_MONTHS",
            "4_MONTHS",
            "5_MONTHS",
            "6_MONTHS",
            "7_MONTHS",
            "8_MONTHS",
            "9_MONTHS",
            "10_MONTHS",
            "11_MONTHS",
            "12_MONTHS"
        };

        public static class PraxisUserListReport
        {
            public static List<string> TranslationKeys { get; set; } = new List<string>
            {
                "YES",
                "NO",
                "APP_USER_MANAGEMENT.ACTIVE",
                "APP_USER_MANAGEMENT.CONTACT_INFO",
                "APP_USER_MANAGEMENT.DATE",
                "APP_USER_MANAGEMENT.DESIGNATION",
                "APP_USER_MANAGEMENT.EMAIL",
                "APP_USER_MANAGEMENT.INACTIVE",
                "APP_USER_MANAGEMENT.NAME",
                "APP_USER_MANAGEMENT.ORGANIZATION",
                "APP_USER_MANAGEMENT.REPORT_NAME",
                "APP_USER_MANAGEMENT.ROLE",
                "APP_USER_MANAGEMENT.STATUS",
                "APP_USER_MANAGEMENT.DATE_OF_JOINING",
                "APP_USER_MANAGEMENT.INTERNAL_NUMBER",
                "APP_USER_MANAGEMENT.CAN_CREATE_PROCESS_GUIDE",
                "APP_USER_MANAGEMENT.GENDER",
                "APP_USER_MANAGEMENT.DATE_OF_BIRTH",
                "APP_USER_MANAGEMENT.NATIONALITY",
                "APP_USER_MANAGEMENT.NATIVE_LANGUAGE",
                "APP_USER_MANAGEMENT.OTHER_LANGUAGE",
                "APP_USER_MANAGEMENT.ACADEMIC_TITLE",
                "APP_USER_MANAGEMENT.WORKLOAD",
                "APP_USER_MANAGEMENT.NUMBER_OF_CHILDREN",
                "APP_USER_MANAGEMENT.TELEPHONE",
                "APP_USER_MANAGEMENT.GLN_NUMBER",
                "APP_USER_MANAGEMENT.ZSR_NUMBER",
                "APP_USER_MANAGEMENT.K-NUMBER",
                "APP_USER_MANAGEMENT.MALE",
                "APP_USER_MANAGEMENT.FEMALE"
            };

            public const int ColumnsForAllDataReport = 22;
            public const int ColumnsForSpecificClientReport = 21;
            public const int HeaderRowIndexForAllDataReport = 3;
            public const int HeaderRowIndexForSpecificClientReport = 4;
            public const int RowHeight = 20;
            public const int LogoSize = 2;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);
        }

        public static class PraxisRiskOverviewReport
        {
            public static List<string> TranslationKeys { get; set; } = new List<string>
            {
                "APP_RISK_MANAGEMENT.ASSESSMENT",
                "APP_RISK_MANAGEMENT.CATEGORY",
                "APP_RISK_MANAGEMENT.CAUSES",
                "APP_RISK_MANAGEMENT.DATE",
                "APP_RISK_MANAGEMENT.EVENT",
                "APP_RISK_MANAGEMENT.IMPACT",
                "APP_RISK_MANAGEMENT.LAST_ASSESSMENT",
                "APP_RISK_MANAGEMENT.MEASURE",
                "APP_RISK_MANAGEMENT.MEASURES_PENDING",
                "APP_RISK_MANAGEMENT.MEASURES_TAKEN",
                "APP_RISK_MANAGEMENT.NUMBER_OF_MEASURES_PENDING",
                "APP_RISK_MANAGEMENT.NUMBER_OF_MEASURES_TAKEN",
                "APP_RISK_MANAGEMENT.PROBABILITY",
                "APP_RISK_MANAGEMENT.REMARKS",
                "APP_RISK_MANAGEMENT.REPORT_NAME",
                "APP_RISK_MANAGEMENT.RISK_ASSESSMENT",
                "APP_RISK_MANAGEMENT.RISK_NAME",
                "APP_RISK_MANAGEMENT.RISK_OWNERS",
                "APP_RISK_MANAGEMENT.RISK_PROFESSIONALS",
                "APP_RISK_MANAGEMENT.SUB_CATEGORY",
                "APP_RISK_MANAGEMENT.TARGET_VALUE",
                "APP_RISK_MANAGEMENT.TOPIC",
                "APP_RISK_MANAGEMENT.VALUE",
                "APP_RISK_MANAGEMENT.JUSTIFIABLE",
                "APP_RISK_MANAGEMENT.CONDITIONALLY_JUSTIFIABLE",
                "APP_RISK_MANAGEMENT.NOT_JUSTIFIABLE",
                "APP_RISK_MANAGEMENT.INSIGNIFICANT",
                "APP_RISK_MANAGEMENT.LOW",
                "APP_RISK_MANAGEMENT.NOTICEABLE",
                "APP_RISK_MANAGEMENT.CRITICAL",
                "APP_RISK_MANAGEMENT.CATASTROPHIC",
                "APP_RISK_MANAGEMENT.UNLIKELY",
                "APP_RISK_MANAGEMENT.VERY_RARE",
                "APP_RISK_MANAGEMENT.RARE",
                "APP_RISK_MANAGEMENT.POSSIBLE",
                "APP_RISK_MANAGEMENT.FREQUENTLY",
                "AVOID_RISK",
                "MITIGATE_RISK",
                "MONITOR_RISK",
                "ACCEPT_RISK",
                "DATE",
                "ORGANIZATION"
            };

            public const int ColumnsForMultipleClientReport = 22;
            public const int ColumnsForSingleClientReport = 21;
            public const int HeaderRowIndexForMultipleClientReport = 4;
            public const int HeaderRowIndexForSingleClientReport = 5;
            public const int RowHeight = 20;
            public const int LogoSize = 2;
            public const string DefaultFontName = "Calibri";
            public const int DefaultFontSize = 11;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);
        }

        public static List<string> PaymentInvoiceTranslationsKeys { get; set; } = new List<string>()
        {
            "APP_PRICING.DUE_DATE",
            "APP_PRICING.INVOICE",
            "APP_PRICING.NUMBER_OF_USER",
            "APP_PRICING.PAY_WITHIN",
            "APP_PRICING.PER_USER",
            "APP_PRICING.RQ_MONITOR_SUBSCRIPTION",
            "APP_PRICING.SERVICE",
            "APP_PRICING.SUBTOTAL",
            "APP_PRICING.TAX",
            "APP_PRICING.TOTAL",
            "APP_PRICING.TOTAL_PAYABLE",
            "APP_PRICING.USERS",
            "RQ_MONITOR",
            "PROCESS_GUIDE",
            "COMPLETE_PACKAGE",
            "APP_PRICING.SUBSCRIPTION_DURATION",
            "APP_PRICING.ALREADY_PAID",
            "APP_PRICING.ADDITIONAL_STORAGE_COST",
            "OTHER"
        };

        public static List<string> SubscriptionPaymentInvoiceTranslationsKeys { get; set; } = new List<string>()
        {
            "APP_PRICING.SUBSCRIPTION_INVOICE",
            "APP_PRICING.SUBSCRIPTION_INVOICE_DATE",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TO",
            "APP_PRICING.SUBSCRIPTION_INVOICE_FROM",
            "APP_PRICING.SUBSCRIPTION_INVOICE_PAID_ON",
            "APP_PRICING.SUBSCRIPTION_INVOICE_VALID_TILL",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_SERVICE",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_QTY",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_RATE",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_LINE_TOTAL",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_TOTAL_USERS",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_NUMBER_OF_FULL_TIME_USERS",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_TOTAL_TOKENS",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_TOTAL_TOKENS_IN_MILLIONS",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_ADDITIONAL_STORAGE",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_ADDITIONAL_STORAGE_IN_GB",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_SUBSCRIPTION_PERIOD",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_SUBSCRIPTION_PERIOD_TOTAL_NUMBER_OF_MONTHS",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_SUBTOTAL",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_VAT",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_TOTAL",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_AMOUNT_DUE",
            "APP_PRICING.SUBSCRIPTION_INVOICE_PAYMENT_METHOD",
            "APP_PRICING.SUBSCRIPTION_INVOICE_PAYMENT_TYPE",
            "APP_PRICING.SUBSCRIPTION_INVOICE_THANK_YOU",
            "APP_PRICING.SUBSCRIPTION_INVOICE_IS_VALID_FOR",
            "APP_PRICING.SUBSCRIPTION_INVOICE_MONTH",
            "APP_PRICING.SUBSCRIPTION_INVOICE_MONTHS",
            "APP_PRICING.SUBSCRIPTION_INVOICE_EXPIRES",
            "APP_PRICING.SUBSCRIPTION_INVOICE_CONTACT",
            "APP_PRICING.SUBSCRIPTION_INSTALLMENT_PERIOD",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_DURATION",
            "APP_PRICING.SUBSCRIPTION_INVOICE_TABLE_INTALLMENT_PERIOD_REMNINING_OF_MONTHS",
            "APP_PRICING.SUBSCRIPTION_PAYMENT_PERIOD",
            "APP_PRICING.SUBSCRIPTION_INVOICE_LABEL_PAYMENT_TYPE"
        };

        public static class PraxisDeveloperReport
        {
            public const int HeaderRowIndexForAllDataReport = 3;
            public const int HeaderRowIndexForSpecificClientReport = 4;
            public const int LogoSize = 2;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);
        }

        public static class ProcessGuideDetailReportElement
        {
            public static List<string> TranslationKeys { get; set; } = new List<string>
            {
                "APP_PROCESS_GUIDE.CLIENT_NAME",
                "APP_PROCESS_GUIDE.REPORT_NAME",
                "APP_PROCESS_GUIDE.DATE_FILTER",
                "APP_PROCESS_GUIDE.TITLE",
                "APP_PROCESS_GUIDE.CASE_ID",
                "APP_PROCESS_GUIDE.BIRTHDAY",
                "APP_PROCESS_GUIDE.NAME",
                "APP_PROCESS_GUIDE.ASSIGNED_ON",
                "APP_PROCESS_GUIDE.ASSIGNED_ORGANIZATION",
                "APP_PROCESS_GUIDE.TASK_DESCRIPTION",
                "APP_PROCESS_GUIDE.TOPIC",
                "APP_PROCESS_GUIDE.COMPLETED_BY_USER",
                "APP_PROCESS_GUIDE.DATE_OF_COMPLETION",
                "APP_PROCESS_GUIDE.COMPLETION_STATUS",
                "APP_PROCESS_GUIDE.OVERALL_COMPLETION",
                "APP_PROCESS_GUIDE.ATTACHMENT_BY_USER",
                "APP_PROCESS_GUIDE.REMARKS",
                "APP_PROCESS_GUIDE.BUDGET",
                "APP_PROCESS_GUIDE.EFFECTIVE_COST",
                "APP_PROCESS_GUIDE.STATUS",
                "APP_PROCESS_GUIDE.CATEGORY_NAME",
                "APP_PROCESS_GUIDE.SUB_CATEGORY_NAME",
                "APP_PROCESS_GUIDE.DESCRIPTION",
                "APP_PROCESS_GUIDE.ATTACHMENTS",
                "APP_PROCESS_GUIDE.PATIENT_NAME",
            };

            public const int ColumnsForAllDataReport = 21;
            public const int ColumnsForSpecificClientReport = 6;
            public const int HeaderRowIndexForAllDataReport = 4;
            public const int HeaderRowIndexForSpecificClientReport = 4;
            public const int RowHeight = 20;
            public const int LogoSize = 2;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);
        }

        public static class ProcessGuideDeveloperReport
        {
            public static List<string> TranslationKeys { get; set; } = new List<string>
            {
                "APP_FORM.CLIENT_NAME",
                "APP_FORM.REPORT_NAME",
                "APP_FORM.DATE_FILTER",
                "APP_FORM.TITLE",
                "APP_FORM.CREATED_ON",
                "APP_FORM.ASSIGNED_ORGANIZATION",
                "APP_FORM.TASK_DESCRIPTION",
                "APP_FORM.TOPIC",
                "APP_FORM.ATTACHMENTS_INSTRUCTIONS",
                "APP_FORM.REMARKS",
                "APP_FORM.BUDGET"
            };

            public const int ColumnsForAllDataReport = 7;
            public const int ColumnsForSpecificClientReport = 7;
            public const int HeaderRowIndexForAllDataReport = 4;
            public const int HeaderRowIndexForSpecificClientReport = 4;
            public const int RowHeight = 20;
            public const int LogoSize = 2;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);
        }

        public static class ProcessGuideOverviewReport
        {
            public static List<string> TranslationKeys { get; set; } = new List<string>
            {
                "APP_PROCESS_GUIDE.ASSIGNED_ON",
                "APP_PROCESS_GUIDE.ASSIGNED_ORGANIZATION",
                "APP_PROCESS_GUIDE.ASSIGNED_USERS",
                "APP_PROCESS_GUIDE.BIRTHDAY",
                "APP_PROCESS_GUIDE.BUDGET",
                "APP_PROCESS_GUIDE.CASE_ID",
                "APP_PROCESS_GUIDE.CLIENT_NAME",
                "APP_PROCESS_GUIDE.COMPLETED_BY_USER",
                "APP_PROCESS_GUIDE.COMPLETION_STATUS",
                "APP_PROCESS_GUIDE.DATE_FILTER",
                "APP_PROCESS_GUIDE.DATE_OF_COMPLETION",
                "APP_PROCESS_GUIDE.DUE_DATE",
                "APP_PROCESS_GUIDE.EFFECTIVE_COST",
                "APP_PROCESS_GUIDE.NAME",
                "APP_PROCESS_GUIDE.OVERALL_COMPLETION",
                "APP_PROCESS_GUIDE.REPORT_NAME",
                "APP_PROCESS_GUIDE.STATUS",
                "APP_PROCESS_GUIDE.TITLE",
                "APP_PROCESS_GUIDE.FORM_TITLE",
                "APP_PROCESS_GUIDE.FORM_DESCRIPTION",
                "APP_PROCESS_GUIDE.PATIENT_NAME",
                "APP_PROCESS_GUIDE.CATEGORY_NAME",
                "APP_PROCESS_GUIDE.SUB_CATEGORY_NAME",
                "APP_PROCESS_GUIDE.TOPIC"
            };

            public const int ColumnsForOverviewReport = 17;
            public const int ColumnsForOverviewShiftPlanReport = 6;
            public const int ColumnsForSpecificClientReport = 17;
            public const int HeaderRowIndexForAllDataReport = 3;
            public const int HeaderRowIndexForSpecificClientReport = 4;
            public const int RowHeight = 20;
            public const int LogoSize = 2;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);
        }

        public static readonly string LogoLocation = @"/Resources/Images/rq-monitor-latest.png";
        public static readonly string processGuideLogo = @"/Resources/Images/rq-monitor-latest.png";
        public static readonly string processMonitorLogo = @"/Resources/Images/rq-monitor-latest.png";
        public static readonly string rqSystemLogo = @"/Resources/Images/rq-monitor-latest.png";
        public static readonly string rqLatestLogo = @"/Resources/Images/rq-monitor-latest.png";
        
        public static class PraxisReportProgress
        {
            public const string Pending = "pending";
            public const string InProgress = "in-progress";
            public const string Complete = "complete";
            public const string Failed = "failed";
        }

        public static class CirsReport
        {
            public static Dictionary<string, string> CommonKeys { get; set; } = new Dictionary<string, string>
            {
                { "ReportName","APP_CIRS_REPORT.REPORT_NAME" },
                { "Date", "APP_CIRS_REPORT.DATE" },
                { "Yes", "YES" },
                { "No", "NO" },
                { "Anonymous", "APP_CIRS_REPORT.ANONYMOUS" }
            };

            public static Dictionary<string, string> ColumnKeys(CirsDashboardName dashboardName)
            {
                var columnKeys = new Dictionary<string, string>
                {
                    { "CardNumber","APP_CIRS_REPORT.CARD_NUMBER" },
                    { "DashboardName","APP_CIRS_REPORT.REPORTING_MODULE" },
                    { "DateCreated", "APP_CIRS_REPORT.DATE_CREATED" },
                    { "CreatedBy", "APP_CIRS_REPORT.CREATED_BY" },
                    { "LastUpdatedBy", "APP_CIRS_REPORT.LAST_UPDATED_BY" },
                    { "Title", "APP_CIRS_REPORT.TITLE" },
                    { "Keywords", "APP_CIRS_REPORT.KEY_WORDS" },
                    { "Description", "APP_CIRS_REPORT.DESCRIPTION" },
                    { "Stage", "APP_CIRS_REPORT.STAGE" },
                    { "Remarks", "APP_CIRS_REPORT.REMARKS" },
                    { "AssignmentLevel", "APP_CIRS_REPORT.CIRS_ASSIGNMENT_LEVEL" },
                    { "ReportedBy", "APP_CIRS_REPORT.REPORTED_BY" }
                };
                
                foreach (var enumValue in dashboardName.StatusEnumValues())
                {
                    columnKeys.Add(ToPascalCase(enumValue), "APP_CIRS_REPORT." + enumValue);
                }

                columnKeys.Add(
                    ToPascalCase(CirsCommonEnum.INACTIVE.ToString()),
                    "APP_CIRS_REPORT." + CirsCommonEnum.INACTIVE.ToString());

                return columnKeys
                    .Concat(CirsReport.GetDashboardSpecificFields(dashboardName))
                    .Concat(CirsReport.GetAttachmentSpecificFields(nameof(PraxisOpenItem)))
                    .Concat(CirsReport.GetAttachmentSpecificFields(nameof(PraxisProcessGuide)))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            public static Dictionary<string, string> StageKeys => 
                typeof(CirsIncidentStatusEnum).GetEnumValues().Cast<object>()
                   .Concat(typeof(CirsComplainStatusEnum).GetEnumValues().Cast<object>())
                   .Concat(typeof(CirsHintStatusEnum).GetEnumValues().Cast<object>())
                   .Concat(typeof(CirsIdeaStatusEnum).GetEnumValues().Cast<object>())
                   .Concat(typeof(CirsAnotherStatusEnum).GetEnumValues().Cast<object>())
                   .Concat(typeof(CirsFaultStatusEnum).GetEnumValues().Cast<object>())
                   .GroupBy(value => value.ToString())
                   .Select(g => g.First())
                   .Select(value => new KeyValuePair<string, string>(value.ToString(), value.ToString()))
                   .ToDictionary(kv => kv.Key, kv => "APP_CIRS_REPORT." + kv.Value);

            public static Dictionary<string, string> TopicKeys { get; set; } = Enum
                .GetValues<RiqsIncidentTopicEnums>()
                .Select(value => new KeyValuePair<string, string>(value.ToString(), "APP_CIRS_REPORT." + value.ToString()))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            public static Dictionary<string, string> GetDashboardSpecificFields(CirsDashboardName dashboardName)
            {
                return dashboardName switch
                {
                    CirsDashboardName.Incident => new Dictionary<string, string> 
                    {
                        { "Topic", "APP_CIRS_REPORT.TOPIC" },
                        { "Measures", "APP_CIRS_REPORT.MEASURES" }
                    },
                    CirsDashboardName.Complain => new Dictionary<string, string>(),
                    CirsDashboardName.Hint => new Dictionary<string, string> 
                    {
                        { "ReportInternalOffice", "APP_CIRS_REPORT.REPORT_INTERNAL_OFFICE" },
                        { "ReportExternalOffice", "APP_CIRS_REPORT.REPORT_EXTERNAL_OFFICE" }
                    },
                    CirsDashboardName.Idea => new Dictionary<string, string>
                    {
                        { "BenefitOfIdea", "APP_CIRS_REPORT.BENEFIT_OF_THE_IDEA" },
                        { "TargetGroup", "APP_CIRS_REPORT.TARGET_GROUP" },
                        { "FeasibilityAndResourceRequirements", "APP_CIRS_REPORT.FEASIBILITY_AND_RESOURCE_REQUIREMENTS" }
                    },
                    CirsDashboardName.Another => new Dictionary<string, string>
                    {
                         { "ImplementationProposal", "APP_CIRS_REPORT.IMPLEMENTATION_PROPOSAL" }
                    },
                    CirsDashboardName.Fault => new Dictionary<string, string>(),
                    _ => new Dictionary<string, string>()
                };
            }

            public static Dictionary<string, string> GetAttachmentSpecificFields(string attachmentType)
            {
                return attachmentType switch
                {
                    nameof(PraxisProcessGuide) => new Dictionary<string, string>
                    {
                        { "ProcessGuideTitle", "APP_CIRS_REPORT.PROCESS_GUIDE_TITLE" },
                        { "ProcessGuideDateCreated", "APP_CIRS_REPORT.PROCESS_GUIDE_DATE_CREATED" },
                        { "ProcessGuideAssignedUsers", "APP_CIRS_REPORT.PROCESS_GUIDE_ASSIGNED_USERS" },
                        { "ProcessGuideCompletionStatus", "APP_CIRS_REPORT.PROCESS_GUIDE_COMPLETION_STATUS" },
                        { "ProcessGuideCompletionDate", "APP_CIRS_REPORT.PROCESS_GUIDE_COMPLETION_DATE" },
                        { "GuideCompletedBy", "APP_CIRS_REPORT.COMPLETED_BY" }
                    },
                    nameof(PraxisOpenItem) => new Dictionary<string, string>
                    {
                        { "OpenItemTitle", "APP_CIRS_REPORT.OPEN_ITEM_TITLE" },
                        { "OpenItemDateCreated", "APP_CIRS_REPORT.OPEN_ITEM_DATE_CREATED" },
                        { "OpenItemAssignedUsers", "APP_CIRS_REPORT.OPEN_ITEM_ASSIGNED_USERS" },
                        { "OpenItemCompletionStatus", "APP_CIRS_REPORT.OPEN_ITEM_COMPLETION_STATUS" },
                        { "OpenItemCompletionDate", "APP_CIRS_REPORT.OPEN_ITEM_COMPLETION_DATE" },
                        { "OpenItemCompletedBy", "APP_CIRS_REPORT.COMPLETED_BY" }
                    },
                    _ => new Dictionary<string, string>()
                };
            }

            public const int ColumnCount= 28;
            public const int HeaderRowIndex = 3;
            public const int RowHeight = 150;
            public const int LogoSize = 2;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);

            private static string ToPascalCase(string value) =>
                string.Join(string.Empty, value.Split('_')
                    .Select(word => word[..1].ToUpper() + word[1..].ToLower()));
        }

        public static class ShiftPlanReport
        {
            public const int HeaderRowIndex = 4;

            public static Dictionary<string, string> LanguageKeys { get; set; } = new Dictionary<string, string>
            {
                { "DEPARTMENT_NAME","LOGISTICS_ORGANIZATION_NAME" },
                { "REPORT_NAME","APP_CIRS_REPORT.REPORT_NAME" },
                { "DATE", "APP_CIRS_REPORT.DATE" },
                { "JANUARY", "JANUARY_FULLNAME" },
                { "FEBRUARY", "FEBRUARY_FULLNAME" },
                { "MARCH", "MARCH_FULLNAME" },
                { "APRIL", "APRIL_FULLNAME" },
                { "MAY", "MAY_FULLNAME" },
                { "JUNE", "JUNE_FULLNAME" },
                { "JULY", "JULY_FULLNAME" },
                { "AUGUST", "AUGUST_FULLNAME" },
                { "SEPTEMBER", "SEPTEMBER_FULLNAME" },
                { "OCTOBER", "OCTOBER_FULLNAME" },
                { "NOVEMBER", "NOVEMBER_FULLNAME" },
                { "DECEMBER", "DECEMBER_FULLNAME" },
                { "MONDAY", "MONDAY_SHORTNAME" },
                { "TUESDAY", "TUESDAY_SHORTNAME" },
                { "WEDNESDAY", "WEDNESDAY_SHORTNAME" },
                { "THURSDAY", "THURSDAY_SHORTNAME" },
                { "FRIDAY", "FRIDAY_SHORTNAME" },
                { "SATURDAY", "SATURDAY_SHORTNAME" },
                { "SUNDAY", "SUNDAY_SHORTNAME" },
                { "TASK", "TASK_MONITOR_REPORT.TASK" },
                { "ASSIGNED_TO", "APP_PLATFORM_DOCUMENT_MANAGER.ATTACHED_TO" },
                { "ATTACHMENT", "ATTACHMENT" },
                { "ATTACHMENTS", "APP_CIRS_REPORT.ATTACHMENTS" }
            };
        }

        public static class ShiftReport
        {
            public const int HeaderRowIndex = 4;

            public static Dictionary<string, string> LanguageKeys { get; set; } = new Dictionary<string, string>
            {
                { "SHIFT_NAME", "APP_SHIFT_PLANNER.SHIFT_NAME" },
                { "ASSIGNED_GUIDES", "APP_SHIFT_PLANNER.ASSIGNED_PROCESS_GUIDES" },
                { "ATTACHED_DOCUMENTS", "APP_SHIFT_PLANNER.ATTACHED_DOCUMENTS" },
                { "FORMS", "APP_SHIFT_PLANNER.FORMS" },
                { "DEPARTMENT_NAME","LOGISTICS_ORGANIZATION_NAME" },
                { "DATE", "APP_CIRS_REPORT.DATE" },
                { "REPORT_NAME","APP_CIRS_REPORT.REPORT_NAME" },
            };

            public static readonly Dictionary<string, string> PrimaryTableColumnKeys = new Dictionary<string, string>
            {
                { "SHIFT_NAME", "APP_SHIFT_PLANNER.SHIFT_NAME" },
                { "ASSIGNED_GUIDES", "APP_SHIFT_PLANNER.ASSIGNED_PROCESS_GUIDES" },
                { "ATTACHED_DOCUMENTS", "APP_SHIFT_PLANNER.ATTACHED_DOCUMENTS" },
                { "FORMS", "APP_SHIFT_PLANNER.FORMS" }
            };
        }

        public static class LibraryReport
        {
            public const int LogoSize = 2;
            public const int HeaderRowIndex = 3;
            public static readonly Color HeaderBackground = Color.FromArgb(222, 222, 222);

            public static Dictionary<string, string> MetadataKeys { get; set; } = new Dictionary<string, string>
            {
                { "REPORT_NAME","APP_CIRS_REPORT.REPORT_NAME" },
                { "DATE", "APP_CIRS_REPORT.DATE" }
            };

            public static Dictionary<string, string> AllViewPrimaryTableColumnKeys { get; set; } = new Dictionary<string, string>
            {
                { "NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_NAME" },
                { "FOLDER_NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_FOLDER_NAME" },
                { "VERSION","APP_PLATFORM_DOCUMENT_MANAGER.VERSION" },
                { "ASSIGNED_TO","APP_PLATFORM_DOCUMENT_MANAGER.ASSIGNED_TO" },
                { "KEYWORDS","APP_CIRS_REPORT.KEY_WORDS" },
                { "UPLOADED_BY", "APP_PLATFORM_DOCUMENT_MANAGER.UPLOADED_BY" },
                { "APPROVED", "APP_PLATFORM_DOCUMENT_MANAGER.APPROVED" },
                { "ACTIVE", "ACTIVE" }
            };

            public static Dictionary<string, string> ApprovalViewPrimaryTableColumnKeys { get; set; } = new Dictionary<string, string>
            {
                { "NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_NAME" },
                { "FOLDER_NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_FOLDER_NAME" },
                { "VERSION","APP_PLATFORM_DOCUMENT_MANAGER.VERSION" },
                { "UPLOADED_BY","APP_PLATFORM_DOCUMENT_MANAGER.UPLOADED_BY" },
                { "APPROVED","APP_PLATFORM_DOCUMENT_MANAGER.APPROVED" },
                { "RE_APPROVAL","APP_PLATFORM_DOCUMENT_MANAGER.REAPPROVAL" },
                { "KEYWORDS", "APP_CIRS_REPORT.KEY_WORDS" },
                { "STATUS", "LOGISTICS_STATUS_VALUE" },
                { "ACTIVE", "ACTIVE" },
            };

            public static Dictionary<string, string> WordFilesViewPrimaryTableColumnKeys { get; set; } = new Dictionary<string, string>
            {
                { "NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_NAME" },
                { "FOLDER_NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_FOLDER_NAME" },
                { "VERSION", "APP_PLATFORM_DOCUMENT_MANAGER.VERSION" },
                { "UPLOADED", "APP_PLATFORM_DOCUMENT_MANAGER.UPLOADED" },
                { "APPROVED", "APP_PLATFORM_DOCUMENT_MANAGER.APPROVED" },
                { "EDITED", "APP_PLATFORM_DOCUMENT_MANAGER.EDITED" },
                { "ASSIGNED_TO", "APP_PLATFORM_DOCUMENT_MANAGER.ASSIGNED_TO" }
            };

            public static Dictionary<string, string> FormViewPrimaryTableColumnKeys { get; set; } =
                new Dictionary<string, string>
                {
                    { "NAME", "APP_PLATFORM_DOCUMENT_MANAGER.DMS_NAME" },
                    { "FOLDER_NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_FOLDER_NAME" },
                    { "VERSION", "APP_PLATFORM_DOCUMENT_MANAGER.VERSION" },
                    { "ASSIGNED_TO", "APP_PLATFORM_DOCUMENT_MANAGER.ASSIGNED_TO" },
                    { "COMPLETED_BY", "APP_PLATFORM_DOCUMENT_MANAGER.COMPLETED_BY" },
                    { "PENDING_COMPLETION", "APP_PLATFORM_DOCUMENT_MANAGER.PENDING_COMPLETION" },
                    { "DEPARTMENT", "APP_PLATFORM_DOCUMENT_MANAGER.ORGANIZATION" }
                };

            public static Dictionary<string, string> FolderStructurePrimaryTableColumnKeys { get; set; } =
                new Dictionary<string, string>
                {
                    { "FOLDER_PATH", "APP_PLATFORM_DOCUMENT_MANAGER.DMS_FOLDER_PATH" },
                    { "FOLDER_NAME","APP_PLATFORM_DOCUMENT_MANAGER.DMS_FOLDER_NAME" },
                    { "FILE_NAME", "APP_PLATFORM_DOCUMENT_MANAGER.DMS_FILE_NAME" },
                    { "VERSION", "APP_PLATFORM_DOCUMENT_MANAGER.VERSION" },
                    { "ASSIGNED_TO", "APP_PLATFORM_DOCUMENT_MANAGER.ASSIGNED_TO" },
                    { "KEYWORDS", "APP_PLATFORM_DOCUMENT_MANAGER.KEY_WORDS" },
                    { "UPLOADED_BY", "APP_PLATFORM_DOCUMENT_MANAGER.UPLOADED_BY" },
                    { "APPROVED", "APP_PLATFORM_DOCUMENT_MANAGER.APPROVED" },
                    { "ACTIVE", "ACTIVE" }
                };
        }

        public static class LibraryDocumentAssigneeReport
        {
            public const int HeaderRowIndex = 10;

            public enum LanguageKeyEnum
            {
                REPORT_NAME,
                DATE,
                FILE_NAME,
                VERSION,
                ASSIGNED_TO,
                KEYWORDS,
                UPLOADED_BY,
                APPROVED_BY,
                ACTIVE,
                UNIT,
                READ_UNREAD_STATUS,
                READ_DATE
            }
            public static Dictionary<string, string> LanguageKeys { get; } = new Dictionary<string, string>
            {
                { nameof(LanguageKeyEnum.REPORT_NAME), "APP_REPORT.REPORT_NAME" },
                { nameof(LanguageKeyEnum.DATE), "APP_REPORT.DATE" },
                { nameof(LanguageKeyEnum.FILE_NAME), "APP_REPORT.FILE_NAME" },
                { nameof(LanguageKeyEnum.VERSION), "APP_REPORT.VERSION" },
                { nameof(LanguageKeyEnum.ASSIGNED_TO), "APP_REPORT.ASSIGNED_TO" },
                { nameof(LanguageKeyEnum.KEYWORDS), "APP_REPORT.KEYWORDS" },
                { nameof(LanguageKeyEnum.UPLOADED_BY), "APP_REPORT.UPLOADED_BY" },
                { nameof(LanguageKeyEnum.APPROVED_BY), "APP_REPORT.APPROVED_BY" },
                { nameof(LanguageKeyEnum.ACTIVE), "APP_REPORT.ACTIVE" },
                { nameof(LanguageKeyEnum.UNIT), "APP_REPORT.UNIT" },
                { nameof(LanguageKeyEnum.READ_UNREAD_STATUS), "APP_REPORT.READ_UNREAD_STATUS" },
                { nameof(LanguageKeyEnum.READ_DATE), "APP_REPORT.READ_DATE" }
            };
        }

        public static List<string> GetDashboardNameKeys()
        {
            return new List<string>
            {
                { "Complain"},
                { "Incident"},
                { "Hint" },
                { "Another" },
                { "Idea" }
            };
        }

        public static class EquipmentMetaDataKeys
        {
            public const string SerialNumber = "SerialNumber";
            public const string InternalNumber = "InternalNumber";
            public const string InstallationNumber = "InstallationNumber";
            public const string UDINumber = "UDINumber";
            public const string MaintenanceDates = "MaintenanceDates";

        }

        public static List<string> GetDownloadUserDataLanguageKeys()
        {
            return new List<string>
            {
                "User ID",
                "Email",
                "FIRST_NAME",
                "LAST_NAME",
                "APP_INRTERFACE_MANAGER.USER_GENDER",
                "APP_INRTERFACE_MANAGER.USER_DATE_OF_BIRTH",
                "APP_INRTERFACE_MANAGER.USER_NATIONALITY",
                "APP_INRTERFACE_MANAGER.USER_ACADEMIC_TITLE",
                "APP_USER_MANAGEMENT.WORKLOAD",
                "APP_INRTERFACE_MANAGER.USER_PHONE",
                "APP_USER_MANAGEMENT.GLN_NUMBER",
                "APP_USER_MANAGEMENT.ZSR_NUMBER",
                "APP_USER_MANAGEMENT.K_NUMBER",
                "APP_USER_MANAGEMENT.ADDITIONAL_GROUP",
                "Remarks"
            };
        }

        public static List<string> GetDownloadEquipmentDataLanguageKeys()
        {
            return new List<string>
            {
                "Equipment ID",
                "APP_INRTERFACE_MANAGER.NAME",
                "APP_INRTERFACE_MANAGER.EXACT_LOCATION",
                "APP_INRTERFACE_MANAGER.SERIAL_NUMBER",
                "APP_INRTERFACE_MANAGER.UDI_NUMBER",
                "APP_INRTERFACE_MANAGER.INTERNAL_NUMBER",
                "APP_INRTERFACE_MANAGER.INSTALLATION_NUMBER",
                "APP_INRTERFACE_MANAGER.PURCHASE_DATE",
                "APP_INRTERFACE_MANAGER.PLACING_IN_SERVICE",
                "APP_INRTERFACE_MANAGER.ADD_ONS_TITLE",
                "APP_INRTERFACE_MANAGER.ADD_ONS_DESCRIPTION",
                "Remarks"
            };
        }

        public static List<string> GetDownloadSupplierDataLanguageKeys()
        {
            return new List<string>
            {
                "Supplier ID",
                "APP_INRTERFACE_MANAGER.SUPPLIER_NAME",
                "APP_INRTERFACE_MANAGER.SUPPLIER_EMAIL",
                "APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON",
                "APP_INRTERFACE_MANAGER.SUPPLIER_PHONENUMBER",
                "APP_INRTERFACE_MANAGER.SUPPLIER_ADDRESS",
                "APP_INRTERFACE_MANAGER.SUPPLIER_BILLINGADDRESS",
                "APP_INRTERFACE_MANAGER.SUPPLIER_CUSTOMERNUMBER",
                "APP_INRTERFACE_MANAGER.SUPPLIER_VALUEADDEDTAXNUMBER",
                "APP_INRTERFACE_MANAGER.SUPPLIER_POSITION",
            };
        }
    }
}
