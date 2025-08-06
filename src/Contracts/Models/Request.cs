using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.Wopi.Contracts.Models
{
    public class Request : IDisposable
    {
        public Request()
        {
            ExecutionErrors = new List<string>();
            RequestProperties = new Dictionary<string, object>();
        }

        public string TaskId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string TenantId { get; set; }

        public string ConversionPipelineId { get; set; }
        public string OriginalAudioFileId { get; set; }

        public Dictionary<string, object> RequestProperties { get; set; }
        public List<string> ExecutionErrors { get; set; }

        public string ErrorMessage { get; set; }

        public string OauthAccessToken { get; set; }

        //This will dispose the request properties if initilized
        public void Dispose()
        {
            foreach (var value in RequestProperties.Select(property => property.Value as IDisposable))
            {
                value?.Dispose();
            }
        }
    }

    public class ImageResizeSetting
    {
        public string SourceFileId { get; set; }
        public Dimension[] Dimensions { get; set; }
        public ParentEntity[] ParentEntities { get; set; }
    }

    public class Dimension
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ParentEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TagPrefix { get; set; }
    }

    public class TranslationOpenItem
    {
        public string CATEGORY { get; set; }
        public string SUB_CATEGORY { get; set; }
        public string TASK { get; set; }
        public string RESPONSIBLE_PERSON { get; set; }
        public string RESPONSIBLE_GROUP { get; set; }
        public string DUE_DATE { get; set; }
        public string DATE { get; set; }
        public string ACTUAL_BUDGET { get; set; }
        public string BUDGET { get; set; }
        public string REMARKS { get; set; }
        public string OPEN_OR_CLOSED { get; set; }
        public string REPORT_NAME { get; set; }
        public string OPEN { get; set; }
        public string CLOSED { get; set; }
        public string TASK_TYPE { get; set; }
        public string TO_DO_REPORT { get; set; }
        public string ORGANIZATION { get; set; }
        public string PENDING { get; set; }
        public string COMPLETION_CRITERIA { get; set; }
        public string RESPONSE_BY_ALL_MEMBER { get; set; }
        public string RESPONSE_BY_SINGLE_MEMBER { get; set; }
        public string COMPLETION_TIME { get; set; }
    }

    public class TranslationDistinctTaskList
    {
        public string ACTIVE { get; set; }
        public string CATEGORY { get; set; }
        public string DATE { get; set; }
        public string DISTINCT_TASK_REPORT { get; set; }
        public string END_DATE { get; set; }
        public string INACTIVE { get; set; }
        public string ORGANIZATION { get; set; }
        public string RECURRENCE { get; set; }
        public string REPEAT { get; set; }
        public string REPORT_NAME { get; set; }
        public string RESPONSIBLE { get; set; }
        public string START_DATE { get; set; }
        public string STATUS { get; set; }
        public string SUB_CATEGORY { get; set; }
        public string TASK { get; set; }
        public string MAIL_NOTIFICATION { get; set; }
        public string NOTIFY_ON { get; set; }
        public string AVOID_WEEKENDS { get; set; }
        public string MOVE_TASK_TO_NEXT_WORKING_DAY_IF_RECURRENCE_IS_ON_WEEKEND { get; set; }
        public string TASK_RESCHEDULED_NOTIFICATION { get; set; }
    }

    public class TranslationEpuimentList
    {
        public string ADDITIONAL_INFORMATION { get; set; }
        public string USER_ADDITIONAL_INFORMATION { get; set; }
        public string CATEGORY { get; set; }
        public string CONTACT_PERSON { get; set; }
        public string DATE { get; set; }
        public string E_MAIL { get; set; }
        public string EQUIPMENT_MANAGEMENT_REPORT { get; set; }
        public string LAST_MAINTENANCE { get; set; }
        public string LAST_MAINTENANCE_STATUS { get; set; }
        public string LOCATION { get; set; }
        public string MAINTENANCE { get; set; }
        public string MAINTENANCE_BY { get; set; }
        public string MANUFACTURER { get; set; }
        public string NAME { get; set; }
        public string NEXT_MAINTENANCE { get; set; }
        public string NEXT_MAINTENANCE_STATUS { get; set; }
        public string ORGANIZATION { get; set; }
        public string PHONE { get; set; }
        public string PLACING_IN_SERVICE { get; set; }
        public string PURCHASE { get; set; }
        public string REMARKS { get; set; }
        public string REPORT_NAME { get; set; }
        public string SERIAL_NUMBER { get; set; }
        public string SUB_CATEGORY { get; set; }
        public string SUPPLIER { get; set; }
        public string TITLE { get; set; }
        public string DESCRIPTION { get; set; }
        public string PHOTOS { get; set; }
        public string ATTACHMENT { get; set; }
        public string UNIT { get; set; }
        public string INTERNAL_NUMBER { get; set; }
        public string INSTALLATION_NUMBER { get; set; }
        public string UDI_NUMBER { get; set; }
        public string LOCATION_ADDRESS { get; set; }
        public string EXACT_LOCATION { get; set; }

    }

    public class EquipmentMaintenanceListTranslation
    {
        public string REPORT_NAME { get; set; }
        public string DATE { get; set;}
        public string MAINTENANCE_OR_VALIDATION { get; set;}
        public string EQUIPMENT_NAME { get; set;}
        public string REMARKS { get; set; }
        public string MAINTENANCE_START_DATE { get; set; }
        public string MAINTENANCE_END_DATE { get; set; }
        public string MAINTENANCE_STATUS { get; set; }
        public string EXECUTING_GROUP { get; set; }
        public string APPROVER { get; set; }
        public string SUPPLIER { get; set; }
        public string PROCESS_GUIDE_NAME { get; set; }
        public string LIBRARY_FORM_NAME { get; set; }
        public string COMPLETED_BY { get; set; }
        public string APPROVED_BY { get; set; }
        public string EQUIPMENT_MAINTENANCE_REPORT { get; set; }
        public string ORGANIZATION { get; set; }
        public string EQUIPMENT_VALIDATION_REPORT { get; set; }
        public string VALIDATION_START_DATE { get; set; }
        public string VALIDATION_END_DATE { get; set; }
        public string VALIDATION_STATUS { get; set; }
    }

    public class CategoryReportTranslation
    {
        public string CATEGORY_REPORT { get; set; }
        public string REPORT_NAME { get; set; }
        public string DATE { get; set; }
        public string ORGANIZATION { get; set; }
        public string NAME_OF_CATEGORIES { get; set; }
        public string SUB_CATEGORY { get; set; }
    }

    public class TrainingReportTranslation
    {
        public string ACTIVE { get; set; }
        public string ASSIGNED_ON { get; set; }
        public string ASSIGNED_TO { get; set; }
        public string DATE { get; set; }
        public string DUE_DATE { get; set; }
        public string INACTIVE { get; set; }
        public string NAME { get; set; }
        public string NOT_COMPLETED_YET { get; set; }
        public string ORGANIZATION { get; set; }
        public string REPORT_NAME { get; set; }
        public string STATUS { get; set; }
        public string TOPIC { get; set; }
        public string TRAINING_REPORT { get; set; }
        public string MAIN_GROUP { get; set; }
        public string COMPLETE { get; set; }
        public string NUMBER_OF_ATTEMPTS { get; set; }
    }

    public class TrainingDetailsTranslation
    {
        public string REPORT_NAME { get; set; }
        public string DATE { get; set; }
        public string ORGANIZATION { get; set; }
        public string TRAINING_MODULE { get; set; }
        public string TRAINING_STATISTIC_REPORT { get; set; }
        public string NAME { get; set; }
        public string CRITERIA_OF_COMPLETION { get; set; }
        public string SUBMISSION_DATE { get; set; }
        public string NAME_OF_RESPONDENT { get; set; }
        public string RESPONSE_SCORE { get; set; }
        public string STATUS { get; set; }
        public string SUBMISSION { get; set; }
        public string PENDING { get; set; }
        public string COMPLETE { get; set; }
    }

    public class DeveloperReportTranslation
    {
        public string CREATED_BY { get; set; }
        public string DATE { get; set; }
        public string DEVELOPER_REPORT { get; set; }
        public string LAST_UPDATE_ON { get; set; }
        public string NAME { get; set; }
        public string ORGANIZATION { get; set; }
        public string PURPOSE { get; set; }
        public string REPORT_NAME { get; set; }
        public string TOPIC { get; set; }
    }

    public class SuppliersReportTranslation
    {
       
        public string DATE { get; set; }
        public string NAME { get; set; }
        public string CLIENT { get; set; }
        public string SUPPLIER_REPORT { get; set; }
        public string REPORT_NAME { get; set; }
        public string CATEGORY_NAME { get; set; }
        public string EMAIL { get; set; }
        public string CONTACTPERSON { get; set; }
        public string PHONENUMBER { get; set; }
        public string ADDRESS { get; set; }
        public string BILLINGADDRESS { get; set; }
        public string CUSTOMERNUMBER { get; set; }
        public string VALUEADDEDTAXNUMBER { get; set; }
        public string POSITION { get; set; }
    }
}