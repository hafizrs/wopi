using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IUilmResourceKeyService
    {
        void InsertResourceKey(UilmResourceKey uilmResourceKey);
        Task InsertResourceKeys(List<UilmResourceKey> resourceKeys);
        string GetResourceValueByKeyName(string keyName, string language = null);
        Dictionary<string, string> GetResourceValueByKeyName(List<string> keyList, string language = null);
        List<UilmResourceKey> GetUilmResourceKeys(List<string> keyNameList, List<string> appIds = null);
        Task<bool> UpsertUilmResoucekeysFromJsonAsync(string fileId);
        Task<string> DownloadUilmResourceKeysAsJsonAsync();
    }
}