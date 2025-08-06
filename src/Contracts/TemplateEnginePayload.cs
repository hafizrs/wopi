
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts
{
    public class TemplateEnginePayload
    {
        public string TemplateFileId { get; set; }
        public string FileId { get; set; }
        public string FileNameExtension { get; set; }
        public string SubscriptionFilterId { get; set; }
        public bool RaiseEventOnProcessEnding { get; set; }
        public bool NotifyOnProcessEnding { get; set; }
        public SqlQuery[] FilteredSqlQueryDatas { get; set; }
        public MetaData[] MetaDataList { get; set; }
        public IDictionary<string, string> EventReferenceData { get; set; }
    }
    public class MetaData
    {
        public string Name { get; set; }
        public string Value { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(MetaDataDictionaryConverter))]
        public List<Dictionary<string, object>> Values { get; set; }
    }
    
    public class SqlQuery {
        public string EntityName {get;set;}
        public string Text {get;set;}
        public string Key {get;set;}
    }
}
