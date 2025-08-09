using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.Constants;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.Queries.WopiModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule
{
    public class WopiService : IWopiService
    {
        private readonly ILogger<WopiService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly string _localFilePath;
        private readonly string _collaboraBaseUrl;
        private readonly string _defaultFileName;
        private readonly string _defaultAccessToken;
        private readonly string _defaultUserDisplayName;

        public WopiService(
            ISecurityContextProvider securityContextProvider,
            IServiceClient serviceClient,
            IConfiguration configuration,
            ILogger<WopiService> logger)
        {
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
            _logger = logger;
            _localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration["LocalFilePath"] ?? "temp_files");
            _collaboraBaseUrl = configuration["CollaboraBaseUrl"];
            _defaultFileName = configuration["DefaultFileName"] ?? "Document.docx";
            _defaultAccessToken = configuration["DefaultAccessToken"] ?? "default-token-123";
            _defaultUserDisplayName = configuration["DefaultUserDisplayName"] ?? "Anonymous User";

            if (!Directory.Exists(_localFilePath))
            {
                Directory.CreateDirectory(_localFilePath);
            }
        }

        public Task<CreateWopiSessionResponse> CreateWopiSession(CreateWopiSessionCommand command)
        {
            if (string.IsNullOrEmpty(command.FileUrl))
            {
                throw new InvalidOperationException("fileUrl is required");
            }

            SecurityContext? securityContext = null;
            try             
            {
                securityContext = _securityContextProvider.GetSecurityContext();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get security context");
            }
            var sessionId = Guid.NewGuid().ToString();

            var session = new WopiSession
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = securityContext?.UserId,
                TenantId = securityContext?.TenantId,
                Language = securityContext?.Language,
                SessionId = sessionId,
                FileUrl = command.FileUrl,
                UploadUrl = command.UploadUrl,
                UploadHeaders = JsonConvert.SerializeObject(command.UploadHeaders ?? new Dictionary<string, string>()),
                FileName = command.FileName ?? _defaultFileName,
                AccessToken = command.AccessToken ?? _defaultAccessToken,
                UserId = command.UserId ?? securityContext?.UserId,
                UserDisplayName = command.UserDisplayName ?? _defaultUserDisplayName,
                CanEdit = command.CanEdit,
                CreatedAt = DateTime.UtcNow,
                Downloaded = false,
                LocalFilePath = Path.Combine(_localFilePath, $"{sessionId}.docx"),
                Tags = new[] { WopiTag.IsValidWopiSession }
            };

            // Store session in memory (like JavaScript Map)
            WopiSessionStore.Set(sessionId, session);
            //await _repository.SaveAsync(session);

            var wopiSrc = Uri.EscapeDataString($"{_collaboraBaseUrl}/wopi/files/{sessionId}");
            var editUrl = $"{_collaboraBaseUrl}/browser/a8848448cc/cool.html?WOPISrc={wopiSrc}&access_token={command.AccessToken}";

            var wopiSession = new CreateWopiSessionResponse
            {
                SessionId = sessionId,
                EditUrl = editUrl,
                WopiSrc = $"{_collaboraBaseUrl}/wopi/files/{sessionId}",
                AccessToken = command.AccessToken,
                Message = "Session created successfully"
            };

            return Task.FromResult(wopiSession);
        }

        public async Task<WopiFileInfo> GetWopiFileInfo(GetWopiFileInfoQuery query)
        {
            var session = WopiSessionStore.Get(query.SessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication
            if (query.AccessToken != session.AccessToken)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            // Ensure file exists locally
            await EnsureFileExists(query.SessionId);
            var fileInfo = new FileInfo(session.LocalFilePath);
            
            return new WopiFileInfo
            {
                BaseFileName = session.FileName,
                Size = fileInfo.Length,
                OwnerId = session.UserId,
                UserId = session.UserId,
                UserCanWrite = session.CanEdit,
                UserCanRename = false,
                UserCanNotWriteRelative = false,
                Version = DateTime.UtcNow.Ticks.ToString(),
                UserFriendlyName = session.UserDisplayName,
                PostMessageOrigin = _collaboraBaseUrl,
                // Additional permissions for editing
                EnableOwnerTermination = false,
                SupportsLocks = true,
                SupportsGetLock = true,
                SupportsExtendedLockLength = true,
                SupportsCobalt = false,
                SupportsUpdate = session.CanEdit,
                UserCanPresent = false
            };
        }

        public async Task<byte[]> GetWopiFileContent(GetWopiFileContentQuery query)
        {
            var session = WopiSessionStore.Get(query.SessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication
            if (query.AccessToken != session.AccessToken)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            // Get file from local storage
            await EnsureFileExists(query.SessionId);
            
            return await File.ReadAllBytesAsync(session.LocalFilePath);
        }

        public async Task UpdateWopiFile(UpdateWopiFileCommand command)
        {
            var session = WopiSessionStore.Get(command.SessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication
            if (command.AccessToken != session.AccessToken)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            if (!session.CanEdit)
            {
                throw new InvalidOperationException("Edit not allowed");
            }
            
            // Save the updated file locally
            await File.WriteAllBytesAsync(session.LocalFilePath, command.FileContent);
            
            // Upload to external service if configured
            if (!string.IsNullOrEmpty(session.UploadUrl))
            {
                try
                {
                    await UploadFile(command.SessionId, command.FileContent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Upload failed, but file saved locally");
                    // Continue even if upload fails
                }
            }
            
            session.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
            //await _repository.UpdateAsync<WopiSession>(s => s.SessionId == command.SessionId, session);
        }

        public List<WopiSessionResponse> GetWopiSessions(GetWopiSessionsQuery query)
        {
            var sessions = WopiSessionStore.GetAll()
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            return sessions.Select(s => new WopiSessionResponse(s)).ToList();
        }

        public WopiSessionResponse GetWopiSession(GetWopiSessionQuery query)
        {
            var session = WopiSessionStore.Get(query.SessionId);
            if (session == null)
            {
                return null;
            }

            return new WopiSessionResponse(session);
        }

        public async Task DeleteWopiSession(DeleteWopiSessionCommand command)
        {
            var session = WopiSessionStore.Get(command.SessionId);
            if (session != null)
            {
                // Delete local file if exists
                if (File.Exists(session.LocalFilePath))
                {
                    File.Delete(session.LocalFilePath);
                }

                WopiSessionStore.Delete(command.SessionId);
                //await _repository.DeleteAsync<WopiSession>(s => s.SessionId == command.SessionId);
            }
        }

        public async Task EnsureFileExists(string sessionId)
        {
            var session = WopiSessionStore.Get(sessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            if (!session.Downloaded)
            {
                try
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(session.FileUrl)
                    };
                    
                    var response = await _serviceClient.SendToHttpAsync(request);
                    response.EnsureSuccessStatusCode();
                    
                    var fileContent = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(session.LocalFilePath, fileContent);
                    
                    session.Downloaded = true;
                    //await _repository.UpdateAsync<WopiSession>(s => s.SessionId == sessionId, session);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading file");
                    throw;
                }
            }
        }

        public async Task<object> UploadFile(string sessionId, byte[] fileBuffer)
        {
            var session = WopiSessionStore.Get(sessionId);
            if (session == null || string.IsNullOrEmpty(session.UploadUrl))
            {
                return null;
            }

            try
            {
                var uploadHeaders = JsonConvert.DeserializeObject<Dictionary<string, string>>(session.UploadHeaders ?? "{}");
                
                using var content = new ByteArrayContent(fileBuffer);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(session.UploadUrl),
                    Content = content
                };
                
                foreach (var header in uploadHeaders)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _serviceClient.SendToHttpAsync(request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                throw;
            }
        }

        public async Task LockWopiFile(LockWopiFileCommand command)
        {
            var session = WopiSessionStore.Get(command.SessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication
            if (command.AccessToken != session.AccessToken)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            
            // Simple lock implementation
            if (command.WopiOverride != "LOCK" && command.WopiOverride != "UNLOCK" && command.WopiOverride != "REFRESH_LOCK")
            {
                throw new InvalidOperationException("Unsupported operation");
            }
        }
    }
} 