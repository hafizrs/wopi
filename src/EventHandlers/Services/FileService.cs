using EventHandlers.Models;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace EventHandlers.Services
{
    public class FileService : IFileService
    {
        private readonly IRepository repository;
        public FileService(IRepository repo)
        {
            repository = repo;
        }

        public File GetFileInformation(string fileId)
        {
            return repository.GetItem<File>(f => f.ItemId == fileId);
        }
        public List<ParentInfo> GetFileParentEntities(File file)
        {
            return GetParentEntitysFromMetaData(file);
        }

        private List<ParentInfo> GetParentEntitysFromMetaData(File sourceFile)
        {
            return JsonConvert.DeserializeObject<List<ParentInfo>>(sourceFile.MetaData["ParentEntitiesIfo"].Value);
        }

        public List<File> GetConvertedFiles(string fileId)
        {
            List<File> convertedFiles = new List<File>();
            var connections = repository.GetItems<Connection>(c => c.ParentEntityID == fileId).ToList();

            if (connections.Count > ImageDimension.Dimensions.Count)
            {
                connections = connections.OrderByDescending(c => c.CreateDate).Take(ImageDimension.Dimensions.Count).ToList();
            }

            foreach (var connection in connections)
            {
                convertedFiles.Add(new File()
                {
                    ItemId = connection.ChildEntityID,
                    Tags = connection.Tags,
                    TenantId = connection.TenantId,
                    RolesAllowedToRead = connection.RolesAllowedToRead,
                    RolesAllowedToDelete = connection.RolesAllowedToDelete,
                    RolesAllowedToUpdate = connection.RolesAllowedToUpdate,
                    RolesAllowedToWrite = connection.RolesAllowedToWrite,
                    Language = connection.Language,
                    CreateDate = connection.CreateDate,
                    CreatedBy = connection.CreatedBy,
                    LastUpdatedBy = connection.LastUpdatedBy,
                    LastUpdateDate = connection.LastUpdateDate
                });
            }

            return convertedFiles;
        }
    }
}
