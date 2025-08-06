using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos
{
    public class ConnectionResult
    {
        public Connection Connection { get; set; }
        public object Parent { get; set; }
        public object Child { get; set; }
    }
}
