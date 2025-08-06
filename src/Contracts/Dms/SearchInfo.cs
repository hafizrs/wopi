using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class SearchInfo
    {
        public const string DefaultSearchField = "ItemId";

        private RiqsObjectArtifact _objectArtifact;
        private IEnumerable<string> _fields;

        public SearchInfo(RiqsObjectArtifact objectArtifact, IEnumerable<string> fields)
        {
            _objectArtifact = objectArtifact;
    
            if(fields == null || fields.Count() == 0)
            {
                _fields = new string[] { DefaultSearchField };
            }
            else
                _fields = fields;
        }

        public IDictionary<string, object> GetSearchInfo()
        {
            IDictionary<string, object> responseArtifact = new Dictionary<string, object>();
            
            foreach (var field in _fields)
            {
                var propertyInfo = _objectArtifact.GetType().GetProperty(field);

                if (propertyInfo == null) continue;

                var value = propertyInfo.GetValue(_objectArtifact, null);

                responseArtifact.Add(field, value);
            }

            return responseArtifact;
        }

    }

    public class SearchResult
    {
        public SearchResult(IEnumerable<dynamic> result, IEnumerable<string> queuedNodes)
        {
            Data = result;
            QueuedNodes = queuedNodes;
        }

        public IEnumerable<dynamic> Data { get; set; }
        public IEnumerable<string> QueuedNodes { get; }
        public string ExecutionTimeInMs { get; set; }
        public int Count { get; set; }
    }
}
