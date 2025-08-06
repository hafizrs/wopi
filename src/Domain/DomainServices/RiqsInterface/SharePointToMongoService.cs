using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class SharePointToMongoService : ISharePointToMongoService
    {
        private readonly ISSOFileInfoService _ssoFileInfoService;
        private readonly IMongoRepository _mongoRepository;
        private readonly ILogger<SharePointToMongoService> _logger;

        public SharePointToMongoService(
            ISSOFileInfoService ssoFileInfoService,
            IMongoRepository mongoRepository,
            ILogger<SharePointToMongoService> logger)
        {
            _ssoFileInfoService = ssoFileInfoService;
            _mongoRepository = mongoRepository;
            _logger = logger;
        }

        public async Task<bool> TransferFileToMongo(string sharePointSite, string filePath)
        {
            try
            {

                var fileContent = await _ssoFileInfoService.GetSSOFileInfo(sharePointSite, filePath);

                if (string.IsNullOrEmpty(fileContent))
                {
                    _logger.LogError("Failed to retrieve file content from SharePoint.");
                    return false;
                }


                var fileDocument = new FileDocument
                {
                    FilePath = filePath,
                    FileContent = fileContent,
                    UploadedAt = DateTime.UtcNow
                };


                _mongoRepository.Save(fileDocument);

                _logger.LogInformation("File successfully transferred to MongoDB.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error transferring file to MongoDB: {ex.Message}");
                throw;
            }
        }
    }



}
