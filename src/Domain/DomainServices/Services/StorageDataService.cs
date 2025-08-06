using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Utils;
using File = Selise.Ecap.Entities.PrimaryEntities.StorageService.File;
using Azure.Core;
using System.Threading;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class StorageDataService : IStorageDataService
    {
        private readonly IServiceClient _serviceClient;
        private readonly ILogger<StorageDataService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly AccessTokenProvider _accessTokenProvider;
        private  string _storageServiceBaseUrl;
        private readonly string _storageVersion;
        private readonly string _storageServicePath;
        private readonly string _conversionPipelinePath;
        private readonly string _moveFilePath;
        private readonly string _storageServiceDeleteFilePath = "/StorageService/StorageCommand/DeleteAll";
        
       public StorageDataService(
            IServiceClient serviceClient,
            ILogger<StorageDataService> logger,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider,
            AccessTokenProvider accessTokenProvider
        )
        {
            _serviceClient = serviceClient;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _accessTokenProvider = accessTokenProvider;
            _storageServiceBaseUrl = configuration["StorageServiceBaseUrl"];
            _storageVersion = configuration["StorageServiceBaseUrl_Version"];
            _storageServicePath = configuration["StorageServicePath"];
            _conversionPipelinePath = configuration["ConversionPipelinePath"];
            _moveFilePath = configuration["StorageServiceMoveFilePath"];
        }

        public async Task<bool> ConvertFileByConversionPipeline(ConversionPipelinePayload payload)
        {
            try
            {
                var httpResponse = await _serviceClient.SendToHttpAsync<CommandResponse>(
                                HttpMethod.Post,
                                _storageServiceBaseUrl,
                                _storageVersion,
                                _conversionPipelinePath,
                                payload,
                                await GetAdminToken());
                if ((httpResponse.HttpStatusCode.Equals(HttpStatusCode.OK) || httpResponse.HttpStatusCode.Equals(HttpStatusCode.Accepted))
                    && httpResponse.StatusCode.Equals(0) && !httpResponse.ErrorMessages.Any())
                {
                    _logger.LogInformation("FIle has been successfully converted by storage service.");
                    return true;
                }
                else
                {
                    var errMsg = $"Response from storage service: {string.Join(",", httpResponse.Errors)}";
                    _logger.LogError("Response from storage service: {ErrorMessage}", errMsg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during convert file with payload: {Payload}. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    JsonConvert.SerializeObject(payload), ex.Message, ex.StackTrace);
                return false;
            }
        }

        public File GetFileInfo(string fileId, bool useImpersonation = false)
        {
            throw new System.NotImplementedException();
        }

        public Stream GetFileContentStream(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return null;

            using var wc = new WebClient();
            var data = wc.DownloadData(fileUrl);

            return new MemoryStream(data);
        }

        public string GetResourceUrl(string entityName, string itemId, string tag, bool useImpersonation = false)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> UploadFileAsync(string fileId, string fileName, byte[] byteArray, string[] tags = null, Dictionary<string, MetaValue> metaData = null, string directoryId = "")
        {
            return await UploadFileAsync(null, fileId, fileName, byteArray, tags, metaData, directoryId);
        }

        public async Task<GetPreSignedUrlForUploadResponse> GetPreSignedUrlForUploadQueryModel(
            PreSignedUrlForUploadQueryModel preSignedUrlForUploadQueryModel, bool useImpersonation = false)
        {
            var token = _securityContextProvider.GetSecurityContext().OauthBearerToken;
            return await GetPreSignedUrlForUploadQueryModel(preSignedUrlForUploadQueryModel, token);
        }

        public bool UploadFileToStorageByUrl(string uploadUrl, byte[] byteArray)
        {
            using var client = new HttpClient();
            var imageBinaryContent = new ByteArrayContent(byteArray);

            using var req = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            req.Content = imageBinaryContent;
            req.Headers.Add("x-ms-blob-type", "BlockBlob");

            using HttpResponseMessage resp = client.SendAsync(req).Result;
            var resultCode = resp.EnsureSuccessStatusCode().StatusCode;
            _logger.LogInformation("UploadFileToStorageByUrlResponse: -> {ResultCode}", JsonConvert.SerializeObject(resultCode));
            if (resultCode.Equals(HttpStatusCode.OK) || resultCode.Equals(HttpStatusCode.Created))
            {
                return true;
            }
            return false;
        }

        public async Task<bool> UploadFileToStorageByUrlAsync(
            string uploadUrl,
            byte[] bytes,
            CancellationToken token = default)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = new ByteArrayContent(bytes)
            };
            request.Headers.Add("x-ms-blob-type", "BlockBlob");

            using var response = await client.SendAsync(
                                     request,
                                     HttpCompletionOption.ResponseHeadersRead,
                                     token).ConfigureAwait(false);

            var code = response.EnsureSuccessStatusCode().StatusCode;
            _logger.LogInformation("Blob upload → {StatusCode}", (int)code);

            return code is HttpStatusCode.OK or HttpStatusCode.Created;
        }

        public async Task<GetPreSignedUrlForUploadResponse> GetPreSignedUrlForUploadQueryModel(PreSignedUrlForUploadQueryModel preSignedUrlForUploadQueryModel,
            string accessToken)
        {
           
            var httpResponse = await _serviceClient.SendToHttpAsync<GetPreSignedUrlForUploadResponse>(
                HttpMethod.Post,
                _storageServiceBaseUrl,
                _storageVersion,
                _storageServicePath,
                preSignedUrlForUploadQueryModel,
                accessToken);

            _logger.LogInformation("GetPreSignedUrlForUploadQueryModel -> {@Response}", httpResponse);
            if (httpResponse is { StatusCode: 0 }) return httpResponse;

            _logger.LogInformation("Failed to get preSignedUrl payload {SerializeObject}", JsonConvert.SerializeObject(httpResponse?.HttpStatusCode));

            return new GetPreSignedUrlForUploadResponse();

        }

        public async Task<bool> UploadFileAsync(
            string accessToken,
            string fileId,
            string fileName,
            byte[] byteArray,
            string[] tags = null,
            Dictionary<string, MetaValue> metaData = null,
            string directoryId = "")
        {
            try
            {
                metaData ??= new Dictionary<string, MetaValue>
                {
                    ["FileInfo"] = new MetaValue
                    {
                        Type = "Value",
                        Value = fileName
                    }
                };

                var metaDataObj = JsonConvert.SerializeObject(metaData);

                var fileTagsJson = JsonConvert.SerializeObject(tags ?? new[] { "File" });

                var preSignedUrlForUploadQueryModel = new PreSignedUrlForUploadQueryModel
                {
                    ItemId = fileId,
                    Name = fileName,
                    Tags = fileTagsJson,
                    MetaData = metaDataObj,
                    ParentDirectoryId = string.IsNullOrEmpty(directoryId) ? "" : directoryId
                };
                GetPreSignedUrlForUploadResponse response;
                if (string.IsNullOrEmpty(accessToken))
                {
                    response =
                        await GetPreSignedUrlForUploadQueryModel(preSignedUrlForUploadQueryModel, true);
                }
                else
                {
                    response =
                        await GetPreSignedUrlForUploadQueryModel(preSignedUrlForUploadQueryModel, accessToken);

                }

                _logger.LogInformation("GetPreSignedUrlForUploadResponse -> {@Response}", response);

                if (response != null && !string.IsNullOrEmpty(response.UploadUrl))
                {
                    return await UploadFileToStorageByUrlAsync(response.UploadUrl, byteArray);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Got Exception in UploadFileAsync. Message: {ErrorMessage}. StackTrace: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return false;
        }

        public async Task<bool> DeleteFile(List<string> fileIds, string accessToken)
        {
            try
            {
                var payload = new
                {
                    ItemIds = fileIds.ToArray()
                };
                var response = await _serviceClient.SendToHttpAsync<CommandResponse>(
                    HttpMethod.Post,
                    _storageServiceBaseUrl,
                    _storageVersion,
                    _storageServiceDeleteFilePath,
                    payload,
                    accessToken);

                if (
                    (response.HttpStatusCode.Equals(HttpStatusCode.OK) || response.HttpStatusCode.Equals(HttpStatusCode.Accepted)) &&
                    response.StatusCode.Equals(0) && !response.ErrorMessages.Any())
                {
                    _logger.LogInformation("File deleted successfully from cloud.");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to delete file. Reason: {Reason}.", response.ExternalError);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Got Exception in DeleteFile. Message: {ErrorMessage}. StackTrace: {StackTrace}.", ex.Message, ex.StackTrace);
            }
            return false;
        }

        private static string ForceHttp(string requestUrl)
        {
            var uriBuilder = new UriBuilder(requestUrl);

            var hadDefaultPort = uriBuilder.Uri.IsDefaultPort;

            uriBuilder.Scheme = Uri.UriSchemeHttp;

            uriBuilder.Port = hadDefaultPort ? -1 : uriBuilder.Port;

            return uriBuilder.ToString();
        }
        public async Task<Stream> GetFileStream(File fileData, string token)
        {
            var fileUrl = ForceHttp(fileData.Url);

            var fileDataRequest = new HttpRequestMessage(HttpMethod.Get, fileUrl);

            if (fileData.AccessModifier == AccessModifier.Secure)
            {
                fileDataRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
            }

            var httpResponseMessage = await _serviceClient.SendToHttpAsync(fileDataRequest);

            if (httpResponseMessage.IsSuccessStatusCode == false)
            {
                return Stream.Null;
            }

            var memoryStream = new MemoryStream();

            await httpResponseMessage.Content.CopyToAsync(memoryStream);

            return memoryStream;
        }

        public async Task<string> GetFileContentString(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return string.Empty;

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    using (Stream fileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (StreamReader reader = new StreamReader(fileStream))
                        {
                            return await reader.ReadToEndAsync();
                        }
                    }
                }
            }

            return string.Empty;
        }

        private async Task<string> GetAdminToken()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tokenInfo = new TokenInfo
            {
                UserId = Guid.NewGuid().ToString(),
                TenantId = securityContext.TenantId,
                SiteId = securityContext.SiteId,
                SiteName = securityContext.SiteName,
                Origin = securityContext.RequestOrigin,
                DisplayName = "lalu vulu",
                UserName = "laluvulu@yopmail.com",
                Language = securityContext.Language,
                PhoneNumber = securityContext.PhoneNumber,
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Tenantadmin }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }

        public void UpdateStorageBaseUrl(string _baseUrl)
        {
            _storageServiceBaseUrl = _baseUrl;
        }

        public async Task<bool> UploadFileAsyncAsGzip(string fileId, string fileName, byte[] byteArray, string contentType = "text/html", string contentEncoding = "gzip")
        {
            var metaData = new Dictionary<string, MetaValue>
            {
                ["FileInfo"] = new MetaValue
                {
                    Type = "Value",
                    Value = fileName
                }
            };

            var metaDataObj = JsonConvert.SerializeObject(metaData);
            var fileTagsJson = JsonConvert.SerializeObject(new[] { "File" });

            var preSignedUrlForUploadQueryModel = new PreSignedUrlForUploadQueryModel
            {
                ItemId = fileId,
                Name = fileName,
                Tags = fileTagsJson,
                MetaData = metaDataObj,
                ParentDirectoryId = ""
            };

            var accessToken = _securityContextProvider.GetSecurityContext().OauthBearerToken;
            var response = await GetPreSignedUrlForUploadQueryModel(preSignedUrlForUploadQueryModel, accessToken);

            _logger.LogInformation("PreSignedUrl HTTP status: {StatusCode}", response?.HttpStatusCode);

            if (response != null && !string.IsNullOrEmpty(response.UploadUrl))
            {
                using var client = new HttpClient();

         
                var content = new ByteArrayContent(byteArray);

                if (!string.IsNullOrEmpty(contentType))
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

                if (!string.IsNullOrEmpty(contentEncoding))
                    content.Headers.ContentEncoding.Add(contentEncoding);

                using var req = new HttpRequestMessage(HttpMethod.Put, response.UploadUrl);
                req.Content = content;

                req.Headers.Add("x-ms-blob-type", "BlockBlob");

                using HttpResponseMessage resp = await client.SendAsync(req);
                var resultCode = resp.StatusCode;

                _logger.LogInformation("Upload response code: {StatusCode}", resultCode);

                return resultCode == HttpStatusCode.OK || resultCode == HttpStatusCode.Created;
            }

            return false;
        }
    }
}
