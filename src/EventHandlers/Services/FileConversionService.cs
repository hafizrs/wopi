using EventHandlers.Models;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.StorageService.Commands;
using SeliseBlocks.StorageService.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventHandlers.Services
{
    public class FileConversionService : IFileConversionService
    {
        private readonly IStorageServiceClient storageDataService;
        private readonly IFileService fileService;
        private readonly IRepository repository;
        private readonly ILogger<FileConversionService> logger;

        private const string TagPrefix = "Resize-Image";
        private const string ConversionPipelineId = "ECAP-IRPL-v.1";
        private const string File = "File";

        public FileConversionService(
            IStorageServiceClient storageDataSvc,
            IFileService fileSvc,
            IRepository repo,
            ILogger<FileConversionService> log
            )
        {
            storageDataService = storageDataSvc;
            fileService = fileSvc;
            repository = repo;
            logger = log;
        }

        public void Convert(string fileId, List<string> fileTags, List<string> conversionTags)
        {
            if (conversionTags == null || conversionTags.Count == 0)
            {
                return;
            }

            if (fileTags.Contains(EventHandlers.Models.Tag.FileOfEquipment)
                || 
                fileTags.Contains(EventHandlers.Models.Tag.FileOfOpenItem)
                ||
                fileTags.Contains(EventHandlers.Models.Tag.FileOfProcessGuide)
                ||
                fileTags.Contains(EventHandlers.Models.Tag.FileOfTraining)
                ||
                fileTags.Contains(EventHandlers.Models.Tag.FileOfDeveloper))
            {
                var dimensions = GetDimensions(conversionTags);
                ResizeAndExecuteConversion(fileId, dimensions, TagPrefix);
            }

        }

        private List<Dimension> GetDimensions(List<string> tags)
        {
            List<Dimension> dimensions = new List<Dimension>();
            foreach (var tag in tags)
            {
                dimensions.Add(ImageDimension.Dimensions[tag]);
            }

            return dimensions;
        }

        public void MarkToDeleteConvertedFileMaps(string orgFileId)
        {
            var updateObj = new
            {
                IsMarkedToDelete = true,
                LastUpdateDate = DateTime.UtcNow
            };
            repository.UpdateMany<ConvertedFileMap>(cfm => cfm.OriginalFileId == orgFileId, updateObj);
        }

        private void ResizeAndExecuteConversion(string fileId, List<Dimension> dimensions, string tagPrefix)
        {

            var imageConversionSetting = new ImageConversionSetting
            {
                SourceFileId = fileId,
                Dimensions = dimensions.ToArray(),
                ParentEntities = new[] {
                    new ParentEntity { Id = fileId, Name = File, TagPrefix = tagPrefix }
                },
                KeepCanvasSameWithImage = true,
                UseJpegEncoding = true
            };
            var executeFileConversionPipeline = new ExecuteFileConversionPipelineCommand
            {
                ConversionPipelineId = ConversionPipelineId,
                TaskId = System.Guid.NewGuid().ToString(),
                RequestProperties = new Dictionary<string, object>
                   {
                       {"ImageResizeSetting", imageConversionSetting}
                   }
            };

            storageDataService.ExecuteFileConversionPipeline(executeFileConversionPipeline);
            
        }

        public void AddConvertedFileMaps(string sourceFileId)
        {
            var sourceFile = fileService.GetFileInformation(sourceFileId);

            logger.LogInformation("Found converted file details information. Id:" + sourceFileId);

            var parenetEntites = fileService.GetFileParentEntities(sourceFile);

            if (parenetEntites != null && parenetEntites.Count > 0)
            {
                logger.LogInformation("Found parent enties with source file id:" + sourceFileId + ". Total:" + parenetEntites.Count);
                var convertedFiles = fileService.GetConvertedFiles(sourceFileId);

                if (convertedFiles != null && convertedFiles.Count > 0)
                {
                    List<ConvertedFileMap> convertedFileMaps = new List<ConvertedFileMap>();

                    if (convertedFileMaps.Count > 0)
                    {
                        logger.LogInformation("Prepare converted file maps. Count was " + convertedFileMaps.Count + " source file id was:" + sourceFileId);

                        foreach (var convertedFileMap in convertedFileMaps)
                        {
                            logger.LogInformation("Going to insert converted file map with id:" + convertedFileMap.ItemId + " source file id was:" + sourceFileId);
                            repository.Save<ConvertedFileMap>(convertedFileMap);
                            System.Threading.Thread.Sleep(20);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Did not found any converted file maps fro source file id" + sourceFileId);
                    }
                }
                else
                {
                    logger.LogError("No converted file founds to insert: source file id was:" + sourceFileId);
                }
            }
            else
            {
                logger.LogError("No parent entity found for source file" + sourceFileId);
            }

        }
    }
}
