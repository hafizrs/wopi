using Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Infrastructure
{
    public class ParsedSQLFilter
    {
        public string PropertyName { get; set; }
        public ComplexValue[] Values { get; set; }
        public Operators Operator { get; set; }
    }
}
