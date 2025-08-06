using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.RiqsInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceManagerService : IRiqsInterfaceManagerService
    {
        private readonly ILogger<RiqsInterfaceManagerService> _logger;
        private readonly IRepository _repository;
        private readonly IServiceClient _httpClient;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;
        private const string SharePointBaseUrl = "https://graph.microsoft.com/v1.0/sites";
        private const string GoogleDriveBaseUrl = "https://www.googleapis.com/drive/v3";

        public RiqsInterfaceManagerService(
            ILogger<RiqsInterfaceManagerService> logger,
            IRepository repository,
            IServiceClient httpClient,
            ISecurityContextProvider securityContextProvider,
            IRiqsInterfaceTokenService riqsInterfaceTokenService)
        {
            _logger = logger;
            _repository = repository;
            _httpClient = httpClient;
            _securityContextProvider = securityContextProvider;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
        }

        public async Task<List<GetSiteResponse>> GetSites()
        {
            var tokenInfo = await _riqsInterfaceTokenService.GetInterfaceTokenAsyncByUserId();
            if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.access_token) || string.IsNullOrEmpty(tokenInfo?.provider))
            {
                _logger.LogWarning($"Failed to retrieve token for GetSites.");
                return null;
            }

            string url = tokenInfo.provider.ToLower() switch
            {
                "microsoft" => $"{SharePointBaseUrl}?search=*",
                "google" => $"{GoogleDriveBaseUrl}/drives?fields=drives(id,name)&pageSize=100", // for organization
                //"google" => $"{GoogleDriveBaseUrl}/files?q=mimeType='application/vnd.google-apps.folder'", // for personal
                _ => string.Empty
            };

            var responseString = await SendHttpRequestAsync(url, tokenInfo.access_token);
            if (string.IsNullOrEmpty(responseString))
            {
                _logger.LogError($"Empty response received for provider: {tokenInfo.provider}");
                return null;
            }

            return tokenInfo.provider.ToLower() switch
            {
                "microsoft" => ParseSharePointSitesResponse(responseString),
                "google" => ParseGoogleDrivesResponse(responseString),
                _ => null
            };
        }

        public async Task<List<GetItemsResponse>> GetItems(string siteId, string itemId = null)
        {
            var tokenInfo = await _riqsInterfaceTokenService.GetInterfaceTokenAsyncByUserId();
            if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.access_token) || string.IsNullOrEmpty(tokenInfo?.provider))
            {
                _logger.LogWarning($"Failed to retrieve token for GetItems.");
                return null;
            }

            string driveFolderFilesQuery = "mimeType='application/vnd.google-apps.folder' or mimeType!='application/vnd.google-apps.folder'";

            string url = tokenInfo?.provider.ToLower() switch
            {
                "microsoft" => string.IsNullOrEmpty(itemId)
                    ? $"{SharePointBaseUrl}/{siteId}/drive/root/children"
                    : $"{SharePointBaseUrl}/{siteId}/drive/items/{itemId}/children",

                "google" => $"{GoogleDriveBaseUrl}/files?q={Uri.EscapeDataString(!string.IsNullOrEmpty(itemId) ? $"'{itemId}' in parents" : driveFolderFilesQuery)}" +
                            "&fields=files(id,name,parents,mimeType,createdTime,modifiedTime," +
                            "lastModifyingUser(emailAddress,displayName))" +
                            "&includeItemsFromAllDrives=true&supportsAllDrives=true",

                _ => string.Empty
            };

            var responseString = await SendHttpRequestAsync(url, tokenInfo.access_token);
            if (string.IsNullOrEmpty(responseString))
            {
                _logger.LogError($"Empty response received for provider: {tokenInfo?.provider}");
                return null;
            }

            return tokenInfo?.provider.ToLower() switch
            {
                "microsoft" => ParseSharePointItemsResponse(responseString),
                "google" => ParseGoogleDriveItemsResponse(responseString, itemId),
                _ => null
            };
        }

        private List<GetSiteResponse> ParseSharePointSitesResponse(string responseString)
        {
            var data = JsonSerializer.Deserialize<GetSitesResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return data?.value?.Select(file => new GetSiteResponse
            {
                Id = file.id,
                DisplayName = file.displayName
            }).ToList();
        }

        private List<GetSiteResponse> ParseGoogleDrivesResponse(string responseString)
        {
            var data = JsonSerializer.Deserialize<GetDrivesResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return data?.drives?.Select(file => new GetSiteResponse
            {
                Id = file.id,
                DisplayName = file.name
            }).ToList();
        }

        private List<GetItemsResponse> ParseSharePointItemsResponse(string responseString)
        {
            var data = JsonSerializer.Deserialize<GetSiteItemsResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return data?.value?.Select(file => new GetItemsResponse
            {
                Id = file.id,
                Name = file.name,
                CreatedDateTime = file.createdDateTime,
                LastModifiedDateTime = file.lastModifiedDateTime,
                LastModifiedBy = file.lastModifiedBy != null
                    ? new ModifiedUser
                    {
                        EmailAddress = file.lastModifiedBy.user.email,
                        DisplayName = file.lastModifiedBy.user.displayName
                    }
                    : new ModifiedUser(),
                IsFolder = file.isFolder
            }).Where(IsValidLibraryModuleFile).ToList();
        }

        private List<GetItemsResponse> ParseGoogleDriveItemsResponse(string responseString, string itemId)
        {
            var data = JsonSerializer.Deserialize<GetDriveItemsResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (string.IsNullOrEmpty(itemId))
            {
                var parentIds = data.files?
               .SelectMany(f => f.parents ?? new List<string>())
               .ToHashSet();

                var fileIds = data.files?
                    .Select(f => f.id)
                    .ToHashSet();

                var missingParents = parentIds?.Where(pid => !fileIds.Contains(pid)).ToList();

                var filteredData = data.files?
                    .Where(f => (f.parents == null || !f.parents.Any() || f.parents.Any(p => missingParents.Contains(p))))
                    .ToList();

                return filteredData
                       .Where(file => RiqsInterfaceConstants.IsValidGoogleDriveMimeType(file.mimeType))
                       .Select(file => new GetItemsResponse
                       {
                           Id = file.id,
                           Name = GetGoogleDriveItemName(file),
                           CreatedDateTime = file.createdTime,
                           LastModifiedDateTime = file.modifiedTime,
                           LastModifiedBy = file.lastModifyingUser != null
                           ? new ModifiedUser
                           {
                               EmailAddress = file.lastModifyingUser.emailAddress,
                               DisplayName = file.lastModifyingUser.displayName
                           }
                           : new ModifiedUser(),
                           IsFolder = file.IsFolder
                       }).Where(IsValidLibraryModuleFile).ToList();
            }
            else
            {
                return data?
                       .files?
                       .Where(file => RiqsInterfaceConstants.IsValidGoogleDriveMimeType(file.mimeType))
                       .Select(file => new GetItemsResponse
                       {
                           Id = file.id,
                           Name = GetGoogleDriveItemName(file),
                           CreatedDateTime = file.createdTime,
                           LastModifiedDateTime = file.modifiedTime,
                           LastModifiedBy = file.lastModifyingUser != null
                           ? new ModifiedUser
                           {
                               EmailAddress = file.lastModifyingUser.emailAddress,
                               DisplayName = file.lastModifyingUser.displayName
                           }
                           : new ModifiedUser(),
                           IsFolder = file.IsFolder
                       }).Where(IsValidLibraryModuleFile).ToList();
            }
        }

        private string GetGoogleDriveItemName(DriveItem item)
        {
            if (!item.IsFolder && string.IsNullOrEmpty(Path.GetExtension(item.name)))
            {
               return item.name + (RiqsInterfaceConstants.GoogleDriveMimeTypeToExtension.TryGetValue(item.mimeType, out string ext) ? ext : string.Empty);
            }
            return item.name;
        }

        private bool IsValidLibraryModuleFile(GetItemsResponse item)
        {
            if (item.IsFolder) return true;

            var extension = LibraryModuleFileFormats.GetFileExtension(item.Name);
            var fileFormat = LibraryModuleFileFormats.GetFileFormat(extension);

            return fileFormat != LibraryFileTypeEnum.OTHER;
        }

        private async Task<string> SendHttpRequestAsync(string url, string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Sending request to: {Url}", url);

                var response = await _httpClient.SendToHttpAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP request error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in HTTP request: {ex.Message}");
                return null;
            }
        }
    }
}
