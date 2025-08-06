using System.Collections.Generic;

namespace EventHandlers.Models
{
   public class GeneratePdfUsingTemplateEnginePayload
    {
        public string TemplateStorageId { get; set; }
        public string PdfStorageId { get; set; }
        public string PdfName { get; set; }
        public string FileId { get; set; }
        public List<GetFilteredSqlQueryData> FilteredSqlQueryDatas { get; set; }
        public bool RaiseEvent { get; set; }
        public bool RaiseNotification { get; set; }
        public float FooterHeight { get; set; }
        public float HeaderHeight { get; set; }
        public bool IsPageNumberEnabled { get; set; }
        public string FooterHtmlStorageId { get; set; }
        public string HeaderHtmlStorageId { get; set; }
        public IEnumerable<MetaData> MetaDataList { get; set; }
        public string PdfGenerationProfileId { get; set; }
        public string FirstPageHeaderStorageId { get; set; }
        public string FirstPageFooterStorageId { get; set; }
    }
    
    public class MetaData
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public List<Dictionary<string, object>> Values { get; set; }
    }
}
