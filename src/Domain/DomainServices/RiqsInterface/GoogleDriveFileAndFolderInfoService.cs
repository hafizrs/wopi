using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class GoogleDriveFileAndFolderInfoService : IGoogleDriveFileAndFolderInfoService
    {
        private const string BaseUrl = "https://www.googleapis.com/drive/v3";
        private readonly IServiceClient _httpClient;
        private readonly ILogger<GoogleDriveFileAndFolderInfoService> _logger;
        private readonly IRepository _repository;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;

        public GoogleDriveFileAndFolderInfoService(
            ILogger<GoogleDriveFileAndFolderInfoService> logger,
            IRepository repository,
            IRiqsInterfaceTokenService riqsInterfaceTokenService,
            IServiceClient httpClient)
        {
            _logger = logger;
            _repository = repository;
            _httpClient = httpClient;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
        }

        public async Task<GoogleDriveFileDetails> GetFileInfo(string fileId, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/files/{fileId}?fields=name,size,mimeType,webContentLink";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendToHttpAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var fileDetails = JsonSerializer.Deserialize<GoogleDriveFileDetails>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return fileDetails != null
                    ? new GoogleDriveFileDetails
                    {
                        Id= fileId,
                        Name = string.IsNullOrEmpty(Path.GetExtension(fileDetails.Name))
                                ? fileDetails.Name + (RiqsInterfaceConstants.GoogleDriveMimeTypeToExtension.TryGetValue(fileDetails.MimeType, out string ext) ? ext : string.Empty)
                                : fileDetails.Name,
                        MimeType = fileDetails.MimeType,
                        Size = fileDetails.Size,
                    }
                    : new GoogleDriveFileDetails();
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

        public async Task<byte[]> GetFileContentBytesAsync(string fileId, string fileExtension, string accessToken)
        {
            string exportableMimeType = RiqsInterfaceConstants.GoogleDriveExtensionToExportableMimeType(fileExtension);

            string exportUrl = exportableMimeType != null
                ? $"https://www.googleapis.com/drive/v3/files/{fileId}/export?mimeType={exportableMimeType}"
                : $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";

            var request = new HttpRequestMessage(HttpMethod.Get, exportUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var exportResponse = await _httpClient.SendToHttpAsync(request);
            if (exportResponse.IsSuccessStatusCode)
            {
                var contentStream = await exportResponse.Content.ReadAsStreamAsync();
                using (var memoryStream = new MemoryStream())
                {
                    await contentStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }

            return null;
        }
    }

}
