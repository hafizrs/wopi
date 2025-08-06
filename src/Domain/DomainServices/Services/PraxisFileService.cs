using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Utils;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisFileService : IPraxisFileService
    {
        private readonly IRepository repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private  string _storageServiceBaseUrl;
        private readonly string _storageVersion;
        public PraxisFileService(IRepository repo, ISecurityContextProvider securityContextProvider,
            IServiceClient serviceClient, IConfiguration configuration)
        {
            repository = repo;
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
            _storageServiceBaseUrl = configuration["StorageServiceBaseUrl"];
            _storageVersion = configuration["StorageServiceBaseUrl_Version"];
        }

        public File GetFileInformation(string fileId)
        {
            return repository.GetItem<File>(f => f.ItemId == fileId);
        }

        public async Task<File> GetFileInfoFromStorage(string fileId)
        {
            return await GetFileInfoFromStorage(fileId, _securityContextProvider.GetSecurityContext().OauthBearerToken);
        }

        public async Task<File> GetFileInfoFromStorage(string fileId, string accessToken)
        {
            try
            {

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_storageServiceBaseUrl + _storageVersion +
                                         "/StorageService/StorageQuery/GetFile?FileId=" + fileId),
                };
                request.Headers.Add("Authorization", $"bearer {accessToken}");
                var httpAsync = await _serviceClient.SendToHttpAsync(request);
                return httpAsync.StatusCode.Equals(HttpStatusCode.OK)
                    ? httpAsync.Content.ReadAsAsync<File>().Result
                    : null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<IEnumerable<EcapFile>> GetFilesInfoFromStorage(List<string> fileIds)
        {
            try
            {
                var token = _securityContextProvider.GetSecurityContext().OauthBearerToken;
                var payload = new
                {
                    FileIds = fileIds.ToArray()
                };
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_storageServiceBaseUrl + _storageVersion +
                                         "/StorageService/StorageQuery/GetFiles"),
                };
                request.Headers.Add("Authorization", $"bearer {token}");
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8,
                    "application/json");
                HttpResponseMessage httpAsync = await _serviceClient.SendToHttpAsync(request);
                return httpAsync.IsSuccessStatusCode
                    ? httpAsync.Content.ReadAsAsync<IEnumerable<EcapFile>>().Result
                    : null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<IEnumerable<ClonedFile>> CloneFiles(List<string> fileIds)
        {
            try
            {
                var token = _securityContextProvider.GetSecurityContext().OauthBearerToken;
                var payload = new
                {
                    FileIdsToBeCloned = fileIds.ToArray()
                };
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_storageServiceBaseUrl + _storageVersion +
                                         "/StorageService/StorageCommand/CloneFiles"),
                };
                request.Headers.Add("Authorization", $"bearer {token}");
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8,
                    "application/json");
                HttpResponseMessage httpAsync = await _serviceClient.SendToHttpAsync(request);
                return httpAsync.IsSuccessStatusCode
                    ? httpAsync.Content.ReadAsAsync<IEnumerable<ClonedFile>>().Result
                    : null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<bool> DeleteFilesFromStorage(List<string> fileIds, string accessToken)
        {
            try
            {
                var token = accessToken;
                if (string.IsNullOrEmpty(accessToken))
                {
                    token = _securityContextProvider.GetSecurityContext().OauthBearerToken;
                }

                var payload = new
                {
                    ItemIds = fileIds.ToArray()
                };
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_storageServiceBaseUrl + _storageVersion +
                                         "/StorageService/StorageCommand/DeleteAll"),
                };
                request.Headers.Add("Authorization", $"bearer {token}");
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8,
                    "application/json");
                HttpResponseMessage httpAsync = await _serviceClient.SendToHttpAsync(request);
                return httpAsync.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<CreateDocumentEditUrlResponse> CreateDocumentEditUrl(CreateDocumentEditUrlPayload payload)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://colabora.rashed.app/wopi/create-session"),
            };
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8,
                "application/json");
            HttpResponseMessage httpAsync = await _serviceClient.SendToHttpAsync(request);
            return httpAsync.IsSuccessStatusCode
                ? httpAsync.Content.ReadAsAsync<CreateDocumentEditUrlResponse>().Result
                : null;
        }

        public async Task<bool> DeleteFilesFromStorage(List<string> fileIds)
        {
            return await DeleteFilesFromStorage(fileIds, null);
        }

        public List<PraxisParentInfo> GetFileParentEntities(File file)
        {
            return GetParentEntitysFromMetaData(file);
        }

        private List<PraxisParentInfo> GetParentEntitysFromMetaData(File sourceFile)
        {
            List<PraxisParentInfo> parentEntities = new List<PraxisParentInfo>()
            {
                new PraxisParentInfo()
                {
                    EntityId = sourceFile.MetaData["EntityId"].Value,
                    EntityName = sourceFile.MetaData["EntityName"].Value
                }
            };

            return parentEntities;
        }

        public List<File> GetConvertedFiles(string fileId)
        {
            List<File> convertedFiles = new List<File>();
            var connections = repository.GetItems<Connection>(c =>
                c.ParentEntityID == fileId && c.Tags.Contains(PraxisTag.ResizeImage_1024_1024)).ToList();

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

        public void UpdateStorageBaseUrl(string _baseUrl)
        {
            _storageServiceBaseUrl = _baseUrl;
        }
    }
}
