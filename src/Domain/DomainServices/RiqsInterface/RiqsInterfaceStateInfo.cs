using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceStateInfo
    {
        public string UserId { get; set; }
        public string StateId { get; set; }
        public string Provider { get; set; }
        public string Code { get; set; }
        public void SetCode(string code)
        {
            Code = code;
        }
        public static RiqsInterfaceStateInfo CreateNew(string userId, string provider)
        {
            return new RiqsInterfaceStateInfo
            {
                Provider = provider,
                StateId = Guid.NewGuid().ToString(),
                UserId = userId
            };
        }

        public string Serialize()
        {

            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(Provider);
                    writer.Write(StateId);
                    writer.Write(UserId);

                }

                return Convert.ToBase64String(m.ToArray());
            }
        }

        public static RiqsInterfaceStateInfo InitializeFromBase64EncodedString(string base64EncodedString)
        {
            var bytes = Convert.FromBase64String(base64EncodedString);

            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    string provider = reader.ReadString();
                    string stateId = reader.ReadString();
                    string userId = reader.ReadString();

                    return new RiqsInterfaceStateInfo
                    {
                        UserId = userId,
                        StateId = stateId,
                        Provider = provider,
                        Code = string.Empty
                    };
                }
            }
        }
    }
}

