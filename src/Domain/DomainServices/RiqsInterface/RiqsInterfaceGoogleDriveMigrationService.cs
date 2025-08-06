using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.DmsMigration;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{

    public class RiqsInterfaceGoogleDriveMigrationService : IRiqsInterfaceGoogleDriveMigrationService
    {
        private const string GoogleDriveBaseUrl = "https://www.googleapis.com/drive/v3";
        private const string GoogleDriveBatchBaseUrl = "https://www.googleapis.com/batch/drive/v3";
        private readonly IServiceClient _httpClient;
        private readonly ILogger<RiqsInterfaceGoogleDriveMigrationService> _logger;
        private readonly IRepository _repository;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;
        private readonly IRiqsInterfaceDMSMigrationService _riqsInterfaceDMSMigrationService;
        private readonly ITokenService _tokenService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;

        public RiqsInterfaceGoogleDriveMigrationService(
            ILogger<RiqsInterfaceGoogleDriveMigrationService> logger,
            IRepository repository,
            IRiqsInterfaceTokenService riqsInterfaceTokenService,
            IServiceClient httpClient,
            IRiqsInterfaceDMSMigrationService riqsInterfaceDMSMigrationService,
            ITokenService tokenService,
            ISecurityContextProvider securityContextProvider,
            IServiceClient serviceClient)
        {
            _logger = logger;
            _repository = repository;
            _httpClient = httpClient;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
            _riqsInterfaceDMSMigrationService = riqsInterfaceDMSMigrationService;
            _tokenService = tokenService;
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
        }

        public async Task<bool> ProcessFileMigration(ProcessInterfaceMigrationCommand command, ExternalUserTokenResponse tokenInfo)
        {
            try
            {
                var batchRequest = new BatchRequest
                {
                    AccessToken = tokenInfo.access_token,
                    Requests = new List<BatchRequestItem>()
                };

                var requestId = 0;

                // Add folder requests
                if (command.FolderIds?.Any() == true)
                {
                    foreach (var folderId in command.FolderIds)
                    {
                        if (string.IsNullOrEmpty(folderId)) continue;

                        // Request to get parent folder details
                        batchRequest.Requests.Add(new BatchRequestItem
                        {
                            Id = requestId++.ToString(),
                            Method = "GET",
                            Url = $"/files/{folderId}?fields=id,name,mimeType,parents,size,modifiedTime,webContentLink"
                        });

                        // Request to get files inside the folder
                        batchRequest.Requests.Add(new BatchRequestItem
                        {
                            Id = requestId++.ToString(),
                            Method = "GET",
                            Url = $"/files?q='{folderId}'+in+parents&fields=files(id,name,mimeType,parents,size,modifiedTime,webContentLink)"
                        });
                    }
                }

                // Add file requests
                if (command.FileIds?.Any() == true)
                {
                    foreach (var fileId in command.FileIds)
                    {
                        if (string.IsNullOrEmpty(fileId)) continue;

                        batchRequest.Requests.Add(new BatchRequestItem
                        {
                            Id = requestId++.ToString(),
                            Method = "GET",
                            Url = $"/files/{fileId}?fields=id,name,mimeType,parents,size,modifiedTime,webContentLink"
                        });
                    }
                }

                if (!batchRequest.Requests.Any())
                    return false;

                var result = new GoogleDriveResult();

                while (batchRequest.Requests.Count() > 0)
                {
                    var response = await MakeBatchRequestAsync(batchRequest);
                    ProcessBatchResponse(response, result, batchRequest);
                }

                var interfaceSummaryId = await SaveRiqsInterfaceSummary(tokenInfo.provider, result, command);
                CompleteMigrationAndInitiateDMSUpload(interfaceSummaryId, command);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessFileMigration. Provider: Google");
                return false;
            }
        }

        private async Task<BatchResponse> MakeBatchRequestAsync(BatchRequest batchRequest)
        {
            var boundary = "batch_" + Guid.NewGuid();
            var batchBody = BuildBatchBody(batchRequest, boundary);

            var request = new HttpRequestMessage(HttpMethod.Post, GoogleDriveBatchBaseUrl)
            {
                Content = new StringContent(batchBody)
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue($"multipart/mixed");
            request.Content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", batchRequest.AccessToken);

            var response = await _httpClient.SendToHttpAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return ParseBatchResponse(responseString);
        }

        private string BuildBatchBody(BatchRequest batchRequest, string boundary)
        {
            var sb = new StringBuilder();

            foreach (var item in batchRequest.Requests)
            {
                sb.AppendLine($"--{boundary}");
                sb.AppendLine("Content-Type: application/http");
                sb.AppendLine("Content-Transfer-Encoding: binary");
                sb.AppendLine();
                sb.AppendLine($"{item.Method} {GoogleDriveBaseUrl}{item.Url} HTTP/1.1");
                sb.AppendLine();
            }
            sb.AppendLine($"--{boundary}--");
            return sb.ToString();
        }

        private BatchResponse ParseBatchResponse(string responseString)
        {
            var responses = new List<BatchResponseItem>();
            var parts = responseString.Split(new string[] { "--batch_" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (!part.Contains("HTTP/1.1")) continue;

                var lines = part.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                string id = null;
                int status = 500;
                string bodyContent = null;
                bool isJsonBody = false;
                var bodyLines = new List<string>();

                foreach (var line in lines)
                {
                    if (line.StartsWith("HTTP/1.1"))
                    {
                        status = int.TryParse(line.Split(" ")[1], out var code) ? code : 500;
                    }
                    else if (line.StartsWith("Content-ID:"))
                    {
                        id = line.Split(": ")[1].Trim('<', '>');
                    }
                    else if (line.StartsWith("{") || line.StartsWith("["))
                    {
                        isJsonBody = true;
                    }

                    if (isJsonBody)
                    {
                        bodyLines.Add(line);
                    }
                }

                if (bodyLines.Count > 0)
                {
                    bodyContent = string.Join("\n", bodyLines);
                }

                JsonElement bodyElement = default;
                if (!string.IsNullOrWhiteSpace(bodyContent))
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(bodyContent);
                        bodyElement = jsonDoc.RootElement.Clone();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse JSON body for batch response item ID: {ResponseId}", id);
                    }
                }

                responses.Add(new BatchResponseItem
                {
                    Id = id,
                    Status = status,
                    Body = bodyElement
                });
            }

            return new BatchResponse { Responses = responses };
        }

        private void ProcessBatchResponse(BatchResponse batchResponse, GoogleDriveResult result, BatchRequest batchRequest)
        {
            if (batchResponse?.Responses == null)
            {
                batchRequest.Requests = new List<BatchRequestItem>();

                foreach (var response in batchResponse.Responses)
                {
                    if (response?.Status != 200 || response.Body.ValueKind == JsonValueKind.Undefined)
                        continue;

                    try
                    {
                        if (response.Body.TryGetProperty("id", out _))
                        {
                            string mimeType = GetJsonPropertySafe(response.Body, "mimeType");

                            bool isFolder = mimeType == "application/vnd.google-apps.folder";

                            if (isFolder)
                            {
                                ProcessFolderResponse(response, result);
                            }
                            else
                            {
                                if (RiqsInterfaceConstants.IsValidGoogleDriveMimeType(mimeType))
                                {
                                    ProcessFileResponse(response, result);
                                }
                            }
                        }
                        else if (response.Body.TryGetProperty("files", out _))
                        {
                            ProcessFolderChildResponse(response, result, batchRequest);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing response item ID: {ResponseId}", response.Id);
                    }
                }
            };
        }

        private void ProcessFolderChildResponse(BatchResponseItem response, GoogleDriveResult result, BatchRequest batchRequest)
        {
            if (!response.Body.TryGetProperty("files", out var filesArray) || filesArray.ValueKind != JsonValueKind.Array)
                return;

            int requestId = 0;

            foreach (var item in filesArray.EnumerateArray())
            {
                string id = GetJsonPropertySafe(item, "id");
                string name = GetJsonPropertySafe(item, "name");
                string mimeType = GetJsonPropertySafe(item, "mimeType");
                string parentId = item.TryGetProperty("parents", out var parents) ? parents[0].GetString() : null;

                if (!RiqsInterfaceConstants.IsValidGoogleDriveMimeType(mimeType)) continue;

                bool isFolder = mimeType == "application/vnd.google-apps.folder";

                if (isFolder)
                {
                    var folderDetails = new FolderDetails
                    {
                        Id = id,
                        Name = name,
                        ParentId = parentId,
                        Path = $"/{name}",
                        Size = 0,
                        ChildCount = 0,
                        LastModified = DateTime.UtcNow
                    };

                    result.Folders[id] = folderDetails;

                    // Request to get files inside the folder
                    batchRequest.Requests.Add(new BatchRequestItem
                    {
                        Id = requestId++.ToString(),
                        Method = "GET",
                        Url = $"/files?q='{id}'+in+parents&fields=files(id,name,mimeType,parents,size,modifiedTime,webContentLink)"
                    });
                }
                else
                {
                    long size = TryGetJsonLong(item, "size");
                    DateTime lastModified = TryGetJsonDateTime(item, "modifiedTime");

                    var fileDetails = new FileDetails
                    {
                        Id = id,
                        Name = string.IsNullOrEmpty(Path.GetExtension(name))
                                ? name + (RiqsInterfaceConstants.GoogleDriveMimeTypeToExtension.TryGetValue(mimeType, out string ext) ? ext : string.Empty)
                                : name,
                        ParentId = parentId,
                        Path = $"/{name}",
                        MimeType = mimeType,
                        Size = size,
                        LastModified = lastModified,
                        DownloadUrl = GetJsonPropertySafe(item, "webContentLink")
                    };

                    result.Files[id] = fileDetails;
                }

                // Update parent folder's ChildCount
                if (!string.IsNullOrEmpty(parentId) && result.Folders.TryGetValue(parentId, out var parentFolder))
                {
                    parentFolder.ChildCount++;
                }
            }
        }

        private void ProcessFolderResponse(BatchResponseItem response, GoogleDriveResult result)
        {
            string id = GetJsonPropertySafe(response.Body, "id");
            string name = GetJsonPropertySafe(response.Body, "name");
            string parentId = response.Body.TryGetProperty("parents", out var parents) ? parents[0].GetString() : null;

            var folderDetails = new FolderDetails
            {
                Id = id,
                Name = name,
                ParentId = parentId,
                Path = $"/{name}",
                Size = 0,
                ChildCount = 0,
                LastModified = DateTime.UtcNow
            };

            result.Folders[id] = folderDetails;
        }

        private void ProcessFileResponse(BatchResponseItem response, GoogleDriveResult result)
        {
            string id = GetJsonPropertySafe(response.Body, "id");
            string name = GetJsonPropertySafe(response.Body, "name");
            string mimeType = GetJsonPropertySafe(response.Body, "mimeType");
            string parentId = response.Body.TryGetProperty("parents", out var parents) ? parents[0].GetString() : null;

            long size = TryGetJsonLong(response.Body, "size");
            DateTime lastModified = TryGetJsonDateTime(response.Body, "modifiedTime");

            var fileDetails = new FileDetails
            {
                Id = id,
                Name = string.IsNullOrEmpty(Path.GetExtension(name))
                        ? name + (RiqsInterfaceConstants.GoogleDriveMimeTypeToExtension.TryGetValue(mimeType, out string ext) ? ext : string.Empty)
                        : name,
                ParentId = parentId,
                Path = $"/{name}",
                MimeType = mimeType,
                Size = size,
                LastModified = lastModified,
                DownloadUrl = GetJsonPropertySafe(response.Body, "webContentLink")
            };

            result.Files[id] = fileDetails;
        }

        private string GetJsonPropertySafe(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    return property.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property {PropertyName}", propertyName);
            }
            return string.Empty;
        }

        private long TryGetJsonLong(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    if (property.ValueKind == JsonValueKind.Number)
                        return property.GetInt64();
                    else if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out var parsedValue))
                        return parsedValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing long property {PropertyName}", propertyName);
            }
            return 0;
        }

        private DateTime TryGetJsonDateTime(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    if (DateTime.TryParse(property.GetString(), out var parsedDate))
                        return parsedDate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing DateTime property {PropertyName}", propertyName);
            }
            return DateTime.UtcNow;
        }

        private async Task<string> SaveRiqsInterfaceSummary(string provider, GoogleDriveResult result, ProcessInterfaceMigrationCommand command)
        {
            var summary = new RiqsInterfaceMigrationSummary()
            {
                Provider = provider,
                ItemId = Guid.NewGuid().ToString(),
                ClientId = command.ClientId,
                OrganizationId = command.OrganizationId,
                InterfaceFiles = new List<InterfaceFile>(),
                InterfaceFolders = new List<InterfaceFolder>()
            };

            foreach (var folder in result.Folders.Values)
            {
                summary.InterfaceFolders.Add(new InterfaceFolder
                {
                    CreateDate = DateTime.UtcNow,
                    FolderId = folder.Id,
                    Name = folder.Name,
                    LastModified = folder.LastModified,
                    ParentId = folder.ParentId,
                    Path = folder.Path
                });
            }

            foreach (var file in result.Files.Values)
            {
                var extension = LibraryModuleFileFormats.GetFileExtension(file.Name);
                var fileFormat = LibraryModuleFileFormats.GetFileFormat(extension);

                if (fileFormat == LibraryFileTypeEnum.OTHER) continue;

                summary.InterfaceFiles.Add(new InterfaceFile
                {
                    CreateDate = DateTime.UtcNow,
                    FileId = file.Id,
                    Name = file.Name,
                    LastModified = file.LastModified,
                    ParentId = file.ParentId,
                    Path = file.Path,
                    FileSize = file.Size
                });
            }

            await _repository.SaveAsync(summary);
            return summary.ItemId;
        }

        private void CompleteMigrationAndInitiateDMSUpload(string summaryId, ProcessInterfaceMigrationCommand command)
        {

            var payload = new InterfaceMigrationFolderAndFileCommand
            {
                InterfaceMigrationSummeryId = summaryId,
                NotificationSubscriptionId = command.NotificationSubscriptionId,
                ActionName = command.ActionName,
                Context = command.Context
            };

            _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisDmsConversionQueueName(), payload);
        }

    }

}
