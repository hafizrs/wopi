using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisFileConversionService: IPraxisFileConversionService
    {
        private readonly IPraxisFileService fileService;
        private readonly IRepository repository;
        private readonly ILogger<PraxisFileConversionService> _logger;

        public PraxisFileConversionService(
            IPraxisFileService fileSvc,
            IRepository repo,
            ILogger<PraxisFileConversionService> logger
            )
        {
            fileService = fileSvc;
            repository = repo;
            _logger = logger;
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

        public void AddConvertedFileMaps(string sourceFileId)
        {

            var sourceFile = fileService.GetFileInformation(sourceFileId);

            _logger.LogInformation("Found converted file details information. Id: {SourceFileId}", sourceFileId);

            var parenetEntites = fileService.GetFileParentEntities(sourceFile);

            if (parenetEntites != null && parenetEntites.Count > 0)
            {
                _logger.LogInformation("Found parent entities with source file id: {SourceFileId}. Total: {TotalCount}", sourceFileId, parenetEntites.Count);
                var convertedFiles = fileService.GetConvertedFiles(sourceFileId);

                if (convertedFiles != null && convertedFiles.Count > 0)
                {
                    List<ConvertedFileMap> convertedFileMaps = new List<ConvertedFileMap>();
                    foreach (var parenEntity in parenetEntites)
                    {
                        var convertedFileMap = GetConvertedFileMapForInsert(parenEntity, convertedFiles, sourceFile);
                        convertedFileMaps.Add(convertedFileMap);
                    }

                    if (convertedFileMaps.Count > 0)
                    {
                        _logger.LogInformation("Prepare converted file maps. Count was {Count}, source file id was: {SourceFileId}", convertedFileMaps.Count, sourceFileId);

                        foreach (var convertedFileMap in convertedFileMaps)
                        {
                            _logger.LogInformation("Going to insert converted file map with id: {ItemId}, source file id was: {SourceFileId}", convertedFileMap.ItemId, sourceFileId);
                            repository.Save<ConvertedFileMap>(convertedFileMap);
                            System.Threading.Thread.Sleep(20);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Did not find any converted file maps for source file id {SourceFileId}", sourceFileId);
                    }
                }
                else
                {
                    _logger.LogError("No converted files found to insert: source file id was: {SourceFileId}", sourceFileId);
                }
            }
            else
            {
                _logger.LogError("No parent entity found for source file {SourceFileId}", sourceFileId);
            }

        }

        private ConvertedFileMap GetConvertedFileMapForInsert(PraxisParentInfo parenInfo, List<File> convertedFiles, File sourceFile)
        {
            var convertedFileMap = new ConvertedFileMap()
            {
                ItemId = Guid.NewGuid().ToString(),
                EntityId = parenInfo.EntityId,
                EntityName = parenInfo.EntityName,
                RolesAllowedToDelete = convertedFiles[0].RolesAllowedToDelete,
                RolesAllowedToRead = convertedFiles[0].RolesAllowedToRead,
                RolesAllowedToUpdate = convertedFiles[0].RolesAllowedToUpdate,
                RolesAllowedToWrite = convertedFiles[0].RolesAllowedToWrite,
                CreatedBy = convertedFiles[0].CreatedBy,
                CreateDate = DateTime.UtcNow,
                LastUpdatedBy = convertedFiles[0].LastUpdatedBy,
                LastUpdateDate = DateTime.UtcNow,
                Language = convertedFiles[0].Language,
                Tags = new string[] { PraxisTag.IsValidConvertedFileMap },
                OriginalFileId = sourceFile.ItemId
            };

            foreach (var convetedFile in convertedFiles.Where(c => c.Tags.Contains(PraxisTag.ResizeImage_1024_1024)))
            {
                convertedFileMap.ReportFileId = convetedFile.ItemId;
            }

            return convertedFileMap;
        }
    }
}
