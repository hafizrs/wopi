using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using ImageResizeSetting = Selise.Ecap.SC.PraxisMonitor.Contracts.Models.ImageResizeSetting;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class DocumentUploadAndConversionService : IDocumentUploadAndConversion
    {
        private readonly IStorageDataService _storageDataService;
        private readonly ILogger<DocumentUploadAndConversionService> _logger;

        public DocumentUploadAndConversionService(
            IStorageDataService storageDataService,
            ILogger<DocumentUploadAndConversionService> logger)
        {
            _storageDataService = storageDataService;
            _logger = logger;
        }


        public async Task<bool> UpdateAndConversion(string fileId, string fileName, byte[] byteArray, string[] tags = null, Dictionary<string, MetaValue> metaData = null, string directoryId = "")
        {
            try
            {
                var success = await _storageDataService.UploadFileAsync(fileId, fileName, byteArray, null, metaData);
                if (success)
                {
                    await _storageDataService.ConvertFileByConversionPipeline(PrepareConversionPipelinePayload(fileId, TagName.LogoOfClient, fileId, nameof(File)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during Upload and convert file. Exception Message: {ex.Message}. Excetion Details: {ex.StackTrace}.");
                return false;
            }
            return true;
        }

        public async Task<bool> FileConversion(string fileId, string tagPrefix, string parentEntityId = null, string parentEntityName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(parentEntityId)) parentEntityId = fileId;
                if (string.IsNullOrEmpty(parentEntityName)) parentEntityName = nameof(File);
                var success = await _storageDataService.ConvertFileByConversionPipeline(PrepareConversionPipelinePayload(fileId, tagPrefix, parentEntityId, parentEntityName));
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during Upload and convert file. Exception Message: {ex.Message}. Excetion Details: {ex.StackTrace}.");
                return false;
            }
        }

        private ConversionPipelinePayload PrepareConversionPipelinePayload(string fileId, string tagPrefix, string parentEntityId, string parentEntityName)
        {
            return new ConversionPipelinePayload
            {
                ConversionPipelineId = "ECAP-IRPL-v.1",
                TaskId = Guid.NewGuid().ToString(),
                RequestProperties = new RequestProperties
                {
                    ImageResizeSetting = new ImageResizeSetting
                    {
                        SourceFileId = fileId,
                        Dimensions = new Dimension[]
                        {
                            new Dimension{ Width=40, Height=40},
                            new Dimension{ Width=64, Height=64},
                            new Dimension{ Width=128, Height=128},
                            new Dimension{ Width=256, Height=256},
                            new Dimension{ Width=512, Height=512},
                            new Dimension{ Width=200, Height=200}
                        },
                        ParentEntities = new ParentEntity[]
                        {
                            new ParentEntity
                            {
                                Id=parentEntityId,
                                Name=parentEntityName,
                                TagPrefix= tagPrefix
                            }
                        }
                    }
                }
            };
        }
    }
}
