using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.Contracts.Models
{
    public class AppResponse
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Features { get; set; }
    }
}
