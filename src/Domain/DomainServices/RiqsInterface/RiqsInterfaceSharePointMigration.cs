using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    using Microsoft.Extensions.Logging;
    using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.DmsMigration;
    using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
    using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
    using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
    using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
    using SeliseBlocks.Genesis.Framework.Events;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class RiqsInterfaceSharePointMigrationService : IRiqsInterfaceSharePointMigrationService
    {
        private const string BaseUrl = "https://graph.microsoft.com/v1.0";
        private readonly IServiceClient _httpClient;
        private readonly ILogger<RiqsInterfaceSharePointMigrationService> _logger;
        private readonly IRepository _repository;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;
        private readonly IRiqsInterfaceDMSMigrationService _riqsInterfaceDMSMigrationService;
        private readonly ITokenService _tokenService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;

        public RiqsInterfaceSharePointMigrationService(
            ILogger<RiqsInterfaceSharePointMigrationService> logger,
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
                    AccessToken = tokenInfo.access_token
                };

                var requestId = 0;

                // Add folder requests
                if (command.FolderIds?.Any() == true)
                {
                    foreach (var folderId in command.FolderIds)
                    {
                        if (string.IsNullOrEmpty(folderId)) continue;

                        batchRequest.Requests.Add(new BatchRequestItem
                        {
                            Id = requestId++.ToString(),
                            Method = "GET",
                            Url = $"/sites/{command.SiteId}/drive/items/{folderId}"
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
                            Url = $"/sites/{command.SiteId}/drive/items/{fileId}"
                        });
                    }
                }

                if (!batchRequest.Requests.Any())
                    return false;

               
                var finalResponses = await ExecuteLargeBatchAsync(batchRequest);

                var allResponses = finalResponses
                    .SelectMany(r => r.Responses)
                    .ToList();

                var mergedResponse = new BatchResponse
                {
                    Responses = allResponses
                };

                var result = ProcessBatchResponse(mergedResponse);

                await ProcessSubfoldersIteratively(result, command.SiteId, tokenInfo.access_token);

                var interfaceSummaryId = await SaveRiqsInterfaceSummary(tokenInfo.provider, result, command);
                CompleteMigrationAndInitiateDMSUpload(interfaceSummaryId, command);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessFileMigration. SiteId: {SiteId}", command.SiteId);
                return false;
            }
        }

        private async Task<string> SaveRiqsInterfaceSummary(string provider, SharePointResult result, ProcessInterfaceMigrationCommand command)
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
                    SiteId = command.SiteId,
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
                    SiteId = command.SiteId,
                    LastModified = file.LastModified,
                    ParentId = file.ParentId,
                    Path = file.Path,
                    FileSize = file.Size,
                    DownloadUrl = file.DownloadUrl
                });
            }

            await _repository.SaveAsync(summary);
            return summary.ItemId;
        }

        private async Task<SharePointResult> ProcessSubfoldersIteratively(SharePointResult result, string siteId, string accessToken)
        {
            var folders = result.Folders.Values.ToList();
            List<FileDetails> files;
            SharePointResult childrens;
            while (folders.Any())
            {
                childrens = await GetChildrens(folders.Select(f => f.Id).ToList(), siteId, accessToken);
                folders = new List<FolderDetails>();
                if (childrens != null)
                {
                    folders = childrens.Folders?.Values?.ToList() ?? new List<FolderDetails>();
                    files = childrens.Files?.Values?.ToList() ?? new List<FileDetails>();

                    folders.ForEach(f =>
                    {
                        result.Folders[f.Id] = f;
                    });
                    files.ForEach(f =>
                    {
                        result.Files[f.Id] = f;
                    });
                }
            }


            return result;
        }

        private async Task<SharePointResult> GetChildrens(List<string> folderIds, string siteId, string accessToken)
        {
            try
            {
                var batchRequest = new BatchRequest
                {
                    AccessToken = accessToken
                };

                var requestId = 0;
                folderIds.ForEach(folderId =>
                {
                    batchRequest.Requests.Add(new BatchRequestItem
                    {
                        Id = requestId++.ToString(),
                        Method = "GET",
                        Url = $"/sites/{siteId}/drive/items/{folderId}/children"
                    });
                });

                var finalResponses = await ExecuteLargeBatchAsync(batchRequest);

                var allResponses = finalResponses
                    .SelectMany(r => r.Responses)
                    .ToList();

                var mergedResponse = new BatchResponse
                {
                    Responses = allResponses
                };

                var data = ProcessBatchResponse(mergedResponse);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching to GetChildrens. {message}", ex.Message);
                return null;
            }
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


        private async Task<List<BatchResponse>> ExecuteLargeBatchAsync(BatchRequest originalBatch)
        {
            const int batchLimit = 7;
            var responses = new List<BatchResponse>();

            var allRequests = originalBatch.Requests;
            var accessToken = originalBatch.AccessToken;

            int totalBatches = (int)Math.Ceiling(allRequests.Count / (double)batchLimit);

            for (int i = 0; i < totalBatches; i++)
            {
                var batchChunk = new BatchRequest
                {
                    AccessToken = accessToken,
                    Requests = allRequests
                        .Skip(i * batchLimit)
                        .Take(batchLimit)
                        .ToList()
                };

                var result = await MakeBatchRequestAsync(batchChunk);
                responses.Add(result);

                // Optional: Respect Graph API rate limits
                await Task.Delay(100); // small delay to avoid throttling
            }

            return responses;
        }

        private async Task<BatchResponse> MakeBatchRequestAsync(BatchRequest batchRequest)
        {
            var requestUrl = $"{BaseUrl}/$batch";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                var jsonContent = JsonSerializer.Serialize(batchRequest);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", batchRequest.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendToHttpAsync(request);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                var batchResponse = JsonSerializer.Deserialize<BatchResponse>(responseString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (batchResponse?.Responses == null)
                {
                    _logger?.LogWarning("Received empty or invalid batch response from {RequestUrl}", requestUrl);
                    return new BatchResponse { Responses = new List<BatchResponseItem>() };
                }

                foreach (var item in batchResponse.Responses)
                {
                    if (item.Status >= 400)
                    {
                        _logger?.LogError("Batch item failed: ID={Id}, Status={Status}",
                            item.Id,
                            item.Status);
                    }
                }

                return batchResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request failed while sending batch request to {RequestUrl}", requestUrl);
                return new BatchResponse { Responses = new List<BatchResponseItem>() };
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Failed to deserialize batch response from {RequestUrl}", requestUrl);
                return new BatchResponse { Responses = new List<BatchResponseItem>() };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during batch request to {RequestUrl}", requestUrl);
                return new BatchResponse { Responses = new List<BatchResponseItem>() };
            }
        }


        private SharePointResult ProcessBatchResponse(BatchResponse batchResponse)
        {
            var result = new SharePointResult();

            if (batchResponse?.Responses == null) return result;

            foreach (var response in batchResponse.Responses)
            {
                if (response?.Status != 200 || response.Body.ValueKind == JsonValueKind.Undefined)
                    continue;

                try
                {
                    if (response.Body.TryGetProperty("folder", out _))
                    {
                        ProcessFolderResponse(response, result);
                    }
                    else if (response.Body.TryGetProperty("value", out var value))
                    {
                        ProcessFolderChildrenResponse(response, value, result);
                    }
                    else if (response.Body.TryGetProperty("file", out _))
                    {
                        ProcessFileResponse(response, result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing response item ID: {ResponseId}", response.Id);
                    continue;
                }
            }

            return result;
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

        private void ProcessFolderResponse(BatchResponseItem response, SharePointResult result)
        {
            try
            {
                var haveParentRef = response.Body.TryGetProperty("parentReference", out var parentRef);
                var parentId = haveParentRef ? GetJsonPropertySafe(parentRef, "id") : null;
                var folder = new FolderDetails
                {
                    Id = GetJsonPropertySafe(response.Body, "id"),
                    Name = GetJsonPropertySafe(response.Body, "name"),
                    Path = haveParentRef ? GetJsonPropertySafe(parentRef, "path") : string.Empty,
                    ParentId = parentId,
                    Size = response.Body.TryGetProperty("size", out var size)
                        ? size.GetInt64()
                        : 0,
                    ChildCount = response.Body.TryGetProperty("folder", out var folderProp)
                        && folderProp.TryGetProperty("childCount", out var count)
                        ? count.GetInt32()
                        : 0,
                    Children = new List<ItemChild>()
                };

                var id = GetJsonPropertySafe(response.Body, "id");
                if (!string.IsNullOrEmpty(id))
                {
                    result.Folders[id] = folder;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing folder response");
            }
        }

        private void ProcessFolderChildrenResponse(BatchResponseItem response, JsonElement value, SharePointResult result)
        {
            try
            {
                if (value.GetArrayLength() == 0) return;

                foreach (var item in value.EnumerateArray())
                {
                    try
                    {
                        var haveParentRef = item.TryGetProperty("parentReference", out var parentRef);
                        var parentId = haveParentRef ? GetJsonPropertySafe(parentRef, "id") : null;

                        var child = new ItemChild
                        {
                            Id = GetJsonPropertySafe(item, "id"),
                            Name = GetJsonPropertySafe(item, "name"),
                            Path = haveParentRef ? GetJsonPropertySafe(parentRef, "path") : null,
                            Type = item.TryGetProperty("folder", out _) ? "folder" : "file",
                            Size = item.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                            LastModified = item.TryGetProperty("lastModifiedDateTime", out var lastMod)
                                ? lastMod.GetDateTime()
                                : DateTime.MinValue,
                            DownloadUrl = GetJsonPropertySafe(item, "@microsoft.graph.downloadUrl")
                        };

                        if (child.Type == "folder")
                        {
                            var folder = new FolderDetails
                            {
                                Id = child.Id,
                                Name = child.Name,
                                LastModified = child.LastModified,
                                Path = child.Path,
                                ParentId = parentId,
                                ChildCount = response.Body.TryGetProperty("folder", out var folderProp)
                                && folderProp.TryGetProperty("childCount", out var count)
                                ? count.GetInt32()
                                : 0,
                            };
                            if (!string.IsNullOrEmpty(child.Id))
                            {
                                result.Folders[child.Id] = folder;
                            }
                        }
                        else
                        {
                            var file = new FileDetails
                            {
                                Id = child.Id,
                                Name = child.Name,
                                LastModified = child.LastModified,
                                ParentId = parentId,
                                Path = child.Path,
                                Size = child.Size,
                                DownloadUrl = child.DownloadUrl
                            };
                            if (!string.IsNullOrEmpty(child.Id))
                            {
                                result.Files[child.Id] = file;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing child item");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing folder children response");
            }
        }

        private void ProcessFileResponse(BatchResponseItem response, SharePointResult result)
        {
            try
            {
                var haveParentRef = response.Body.TryGetProperty("parentReference", out var parentRef);
                var parentId = haveParentRef ? GetJsonPropertySafe(parentRef, "id") : null;
                var file = new FileDetails
                {
                    Id = GetJsonPropertySafe(response.Body, "id"),
                    Name = GetJsonPropertySafe(response.Body, "name"),
                    Path = haveParentRef ? GetJsonPropertySafe(parentRef, "path") : string.Empty,
                    ParentId = parentId,
                    Size = response.Body.TryGetProperty("size", out var size)
                        ? size.GetInt64()
                        : 0,
                    MimeType = response.Body.TryGetProperty("file", out var fileProp)
                        ? GetJsonPropertySafe(fileProp, "mimeType")
                        : string.Empty,
                    LastModified = response.Body.TryGetProperty("lastModifiedDateTime", out var lastMod)
                        ? lastMod.GetDateTime()
                        : DateTime.MinValue,
                    DownloadUrl = GetJsonPropertySafe(response.Body, "@microsoft.graph.downloadUrl")
                };

                var id = GetJsonPropertySafe(response.Body, "id");
                if (!string.IsNullOrEmpty(id))
                {
                    result.Files[id] = file;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file response");
            }
        }
    }

}
