using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class SharePointFileAndFolderInfoService : ISharePointFileAndFolderInfoService
    {
        private const string BaseUrl = "https://graph.microsoft.com/v1.0";
        private readonly IServiceClient _httpClient;
        private readonly ILogger<SharePointFileAndFolderInfoService> _logger;
        private readonly IRepository _repository;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;

        public SharePointFileAndFolderInfoService(
            ILogger<SharePointFileAndFolderInfoService> logger,
            IRepository repository,
            IRiqsInterfaceTokenService riqsInterfaceTokenService,
            IServiceClient httpClient)
        {
            _logger = logger;
            _repository = repository;
            _httpClient = httpClient;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
        }

        public async Task<FolderDetails> GetFolderInfo(string siteId, string folderId)
        {
            try
            {
                var tokenInfo = await _riqsInterfaceTokenService.GetInterfaceTokenAsyncByUserId();

                if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.access_token))
                    return null;

                var url = $"{BaseUrl}/sites/{siteId}/drive/items/{folderId}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenInfo.access_token);

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendToHttpAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var folderDetails = JsonSerializer.Deserialize<FolderDetails>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return folderDetails;
                }
                else
                {
                    _logger.LogError("Error: {StatusCode} ", response.StatusCode);
                    return null;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error in GetFolderInfo. FolderId: {folderId}", folderId);
                return null;
            }
        }

        public async Task<FileDetails> GetFileInfo(string siteId, string fileId, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/sites/{siteId}/drive/items/{fileId}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendToHttpAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var fileDetails = JsonSerializer.Deserialize<FileDetails>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return fileDetails;
                }
                else
                {
                    _logger.LogError("Error: {StatusCode} ", response.StatusCode);
                    return null;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error in GetFileInfo. FileId: {fileId}", fileId);
                return null;
            }
        }

        public async Task<byte[]> GetFileContentBytesAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return null;

            using var httpClient = new HttpClient();

            try
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode(); 

                    // Get the stream
                    using (Stream fileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            await fileStream.CopyToAsync(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error downloading file:: {message}", ex.Message);
                return null;
            }
        }
    }

}
