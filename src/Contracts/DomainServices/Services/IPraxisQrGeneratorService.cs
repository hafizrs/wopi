using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisQrGeneratorService
    {
        Task<string> QRCodeGenerateAsync(PraxisEquipment equipmentForQrCode, string qrCodeContent, int height = 100, int width = 100, int margin = 0);
    }
}
