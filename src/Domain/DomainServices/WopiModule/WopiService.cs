using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.Constants;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.Queries.WopiModule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule
{
    public class WopiService : IWopiService
    {
        private readonly ILogger<WopiService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _localFilePath;
        private readonly string _collaboraBaseUrl;
        private readonly string _defaultFileName;
        private readonly string _defaultAccessToken;
        private readonly string _defaultUserDisplayName;
        private readonly string _browserPath;

        public WopiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<WopiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration["LocalFilePath"] ?? "temp_files");
            _collaboraBaseUrl = configuration["CollaboraBaseUrl"] ?? "https://colabora.rashed.app";
            _defaultFileName = configuration["DefaultFileName"] ?? "Document.docx";
            _defaultAccessToken = configuration["DefaultAccessToken"] ?? "default-token-123";
            _defaultUserDisplayName = configuration["DefaultUserDisplayName"] ?? "Anonymous User";
            _browserPath = configuration["BrowserPath"] ?? "a8848448cc";

            // Configure HttpClient timeout (like JavaScript axios timeout)
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // 60 second timeout

            if (!Directory.Exists(_localFilePath))
            {
                Directory.CreateDirectory(_localFilePath);
            }

            // Initialize persistent session storage
            WopiSessionStore.Initialize(_logger);
            
            _logger.LogInformation("WopiService initialized with persistent session storage");
        }

        public async Task<CreateWopiSessionResponse> CreateWopiSession(CreateWopiSessionCommand command)
        {
            if (string.IsNullOrEmpty(command.FileUrl))
            {
                throw new InvalidOperationException("fileUrl is required");
            }
          
            var sessionId = command.SessionId ?? Guid.NewGuid().ToString();

            // Check if session already exists and delete it first
            var existingSession = WopiSessionStore.Get(sessionId);
            if (existingSession != null)
            {
                _logger.LogInformation("Session {SessionId} already exists, deleting old session before creating new one", sessionId);
                
                try
                {
                    // Use the existing delete method
                    var deleteCommand = new DeleteWopiSessionCommand { SessionIds = new[] { sessionId } };
                    await DeleteWopiSession(deleteCommand);
                    _logger.LogInformation("Successfully deleted existing session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete existing session {SessionId}, continuing with new session creation", sessionId);
                }
            }

            // Generate edit URL (matching JavaScript implementation exactly)
            var wopiSrc = Uri.EscapeDataString($"{_collaboraBaseUrl}/wopi/files/{sessionId}");
            var editUrl = $"{_collaboraBaseUrl}/browser/{_browserPath}/cool.html?WOPISrc={wopiSrc}&access_token={command.AccessToken ?? _defaultAccessToken}";

            var session = new WopiSession
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = "system", // Default user since we removed SecurityContext dependency
                TenantId = "default-tenant",
                Language = "en",
                SessionId = sessionId,
                FileUrl = command.FileUrl,
                UploadUrl = command.UploadUrl,
                UploadHeaders = JsonConvert.SerializeObject(command.UploadHeaders ?? new Dictionary<string, string>()),
                FileName = command.FileName ?? _defaultFileName,
                AccessToken = command.AccessToken ?? _defaultAccessToken,
                UserId = command.UserId ?? "default-user",
                UserDisplayName = command.UserDisplayName ?? null,
                CanEdit = command.CanEdit || true, // Ensure CanEdit is true by default
                CreatedAt = DateTime.UtcNow,
                Downloaded = false,
                LocalFilePath = Path.Combine(_localFilePath, $"{sessionId}.docx"),
                Tags = new[] { WopiTag.IsValidWopiSession },
                
                // Store computed properties
                EditUrl = editUrl,
                WopiSrc = $"{_collaboraBaseUrl}/wopi/files/{sessionId}"
            };

            // Store session in memory (like JavaScript Map)
            WopiSessionStore.Set(sessionId, session);
            //await _repository.SaveAsync(session);

            var wopiSession = new CreateWopiSessionResponse
            {
                SessionId = sessionId,
                EditUrl = session.EditUrl,
                WopiSrc = session.WopiSrc,
                AccessToken = session.AccessToken,
                Message = "Session created successfully"
            };

            _logger.LogInformation("Created session {SessionId} for file: {FileUrl}, CanEdit: {CanEdit}, UserCanWrite: {UserCanWrite}", 
                sessionId, command.FileUrl, session.CanEdit, true);

            return wopiSession;
        }

        public async Task<WopiFileInfo> GetWopiFileInfo(GetWopiFileInfoQuery query)
        {
            var session = WopiSessionStore.Get(query.SessionId);
            if (session == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", query.SessionId);
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication
            _logger.LogInformation("CheckFileInfo - Token received: {AccessToken}", query.AccessToken);
            if (query.AccessToken != session.AccessToken)
            {
                _logger.LogWarning("CheckFileInfo - Authentication failed");
                throw new UnauthorizedAccessException("Unauthorized");
            }

            // Ensure file exists locally
            await EnsureFileExists(query.SessionId);
            var fileInfo = new FileInfo(session.LocalFilePath);
            
            _logger.LogInformation("CheckFileInfo - Success for session: {SessionId}, CanEdit: {CanEdit}", 
                query.SessionId, session.CanEdit);
            
            return new WopiFileInfo
            {
                BaseFileName = session.FileName,
                Size = fileInfo.Length,
                OwnerId = session.UserId,
                UserId = session.UserId,
                UserCanWrite = session.CanEdit, // CRITICAL: Must be true for editing
                UserCanRename = false,
                UserCanNotWriteRelative = true,
                Version = DateTime.UtcNow.Ticks.ToString(),
                UserFriendlyName = session.UserDisplayName,
                PostMessageOrigin = _collaboraBaseUrl,
                // Additional permissions for editing
                EnableOwnerTermination = false,
                SupportsLocks = true,
                SupportsGetLock = true,
                SupportsExtendedLockLength = true,
                SupportsCobalt = false,
                SupportsUpdate = true,
                UserCanPresent = false,
                
                // CRITICAL: Set all the missing properties that Collabora needs
                SupportsPutFile = true,           // This is the key one!
                SupportsUnlock = true,
                SupportsRefreshLock = true,
                SupportsGetFile = true,
                SupportsCheckFileInfo = true,
                SupportsDeleteFile = false,
                SupportsRenameFile = false,
                SupportsPutRelativeFile = false,
                SupportsGetFileWopiSrc = false,
                SupportsExecuteCobaltRequest = false,
                SupportsUserInfo = false,
                SupportsFolders = false,
                SupportsFileCreation = false
            };
        }

        public async Task<Stream> GetWopiFileContent(GetWopiFileContentQuery query)
        {
            var session = WopiSessionStore.Get(query.SessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication
            _logger.LogInformation("GetFile - Token received: {AccessToken}", query.AccessToken);
            if (query.AccessToken != session.AccessToken)
            {
                _logger.LogWarning("GetFile - Authentication failed");
                throw new UnauthorizedAccessException("Unauthorized");
            }

            // Get file from local storage
            await EnsureFileExists(query.SessionId);
            
            _logger.LogInformation("GetFile - Streaming file for session: {SessionId}", query.SessionId);
            // Return file stream (like JavaScript fs.createReadStream)
            return new FileStream(session.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        // Direct method matching JavaScript GetFile flow exactly
        public async Task<(Stream fileStream, string fileName)> GetWopiFileContentDirect(string sessionId, string accessToken)
        {
            // Get session (like JavaScript fileConfigs.get(sessionId))
            var session = WopiSessionStore.Get(sessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication (like JavaScript)
            _logger.LogInformation("GetFile - Token received: {AccessToken}", accessToken);
            if (string.IsNullOrEmpty(accessToken) || accessToken != session.AccessToken)
            {
                _logger.LogWarning("GetFile - Authentication failed");
                throw new UnauthorizedAccessException("Unauthorized");
            }

            // Get file from local storage (like JavaScript ensureFileExists)
            await EnsureFileExists(sessionId);
            
            _logger.LogInformation("GetFile - Streaming file for session: {SessionId}", sessionId);
            
            // Return file stream (like JavaScript fs.createReadStream)
            var fileStream = new FileStream(session.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (fileStream, session.FileName);
        }

        public async Task<UpdateWopiFileResponse> UpdateWopiFile(UpdateWopiFileCommand command)
        {
            _logger.LogInformation("UpdateWopiFile called for session: {SessionId}", command.SessionId);
            
            var session = WopiSessionStore.Get(command.SessionId);
            if (session == null)
            {
                _logger.LogWarning("UpdateWopiFile - Session not found: {SessionId}", command.SessionId);
                throw new InvalidOperationException("Session not found");
            }

            // Check for authentication
            if (command.AccessToken != session.AccessToken)
            {
                _logger.LogWarning("UpdateWopiFile - Authentication failed for session: {SessionId}", command.SessionId);
                throw new UnauthorizedAccessException("Unauthorized");
            }

            if (!session.CanEdit)
            {
                _logger.LogWarning("UpdateWopiFile - Edit not allowed for session: {SessionId}", command.SessionId);
                throw new InvalidOperationException("Edit not allowed");
            }
            
            _logger.LogInformation("PutFile - Saving file for session: {SessionId}, File size: {FileSize} bytes", 
                command.SessionId, command.FileContent?.Length ?? 0);
            
            try
            {
                // Save the updated file locally
                await File.WriteAllBytesAsync(session.LocalFilePath, command.FileContent);
                _logger.LogInformation("PutFile - File saved locally for session: {SessionId}", command.SessionId);
                
                // Upload to external service if configured
                object uploadResult = null;
                string uploadStatus = "Not configured";
                
                if (!string.IsNullOrEmpty(session.UploadUrl))
                {
                    try
                    {
                        _logger.LogInformation("PutFile - Uploading to external service: {UploadUrl}", session.UploadUrl);
                        uploadResult = await UploadFile(command.SessionId, command.FileContent);
                        uploadStatus = "Success";
                        _logger.LogInformation("PutFile - File uploaded to external service for session: {SessionId}", command.SessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "PutFile - Upload failed for session: {SessionId}, but file saved locally", command.SessionId);
                        uploadStatus = "Failed";
                        // Continue even if upload fails
                    }
                }
                else
                {
                    _logger.LogInformation("PutFile - No external upload URL configured for session: {SessionId}", command.SessionId);
                }
                
                session.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                session.Downloaded = true; // Mark as downloaded since we now have local content
                //await _repository.UpdateAsync<WopiSession>(s => s.SessionId == command.SessionId, session);
                
                _logger.LogInformation("PutFile - File saved successfully for session: {SessionId}", command.SessionId);
                
                // Return success response (matching JavaScript implementation)
                return new UpdateWopiFileResponse
                {
                    LastModifiedTime = DateTime.UtcNow.ToString("O"), // ISO 8601 format
                    Name = session.FileName,
                    Size = command.FileContent.Length,
                    Version = DateTime.UtcNow.Ticks.ToString(),
                    UploadResult = uploadStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PutFile - Error saving file for session: {SessionId}", command.SessionId);
                throw;
            }
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
            // First try to get from memory store
            var session = WopiSessionStore.Get(query.SessionId);
            if (session != null)
            {
                _logger.LogDebug("Retrieved session from memory: {SessionId}", query.SessionId);
                return new WopiSessionResponse(session);
            }

            // If not in memory, try to load from disk (if you have persistence)
            _logger.LogWarning("Session not found in memory: {SessionId}", query.SessionId);
            return null;
        }

        public async Task DeleteWopiSession(DeleteWopiSessionCommand command)
        {
            if (command.SessionIds == null || command.SessionIds.Length == 0)
            {
                _logger.LogWarning("No session IDs provided for deletion");
                return;
            }

            _logger.LogInformation("Starting batch deletion of {Count} sessions", command.SessionIds.Length);

            // Batch process file deletions for better performance
            var fileDeletionTasks = new List<Task>();
            var sessionsToDelete = new List<string>();

            foreach (var sessionId in command.SessionIds)
            {
                if (string.IsNullOrEmpty(sessionId))
                    continue;

                var session = WopiSessionStore.Get(sessionId);
                if (session != null)
                {
                    sessionsToDelete.Add(sessionId);
                    
                    // Queue file deletion task
                    if (File.Exists(session.LocalFilePath))
                    {
                        var filePath = session.LocalFilePath;
                        fileDeletionTasks.Add(Task.Run(() => 
                        {
                            try
                            {
                                File.Delete(filePath);
                                _logger.LogDebug("Deleted local file for session: {SessionId}", sessionId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error deleting file for session: {SessionId}", sessionId);
                            }
                        }));
                    }
                }
                else
                {
                    _logger.LogWarning("Session not found for deletion: {SessionId}", sessionId);
                }
            }

            // Wait for all file deletions to complete
            if (fileDeletionTasks.Count > 0)
            {
                _logger.LogInformation("Waiting for {Count} file deletions to complete", fileDeletionTasks.Count);
                await Task.WhenAll(fileDeletionTasks);
            }

            // Batch delete from session store
            foreach (var sessionId in sessionsToDelete)
            {
                try
                {
                    WopiSessionStore.Delete(sessionId);
                    //await _repository.DeleteAsync<WopiSession>(s => s.SessionId == sessionId);
                    _logger.LogDebug("Deleted WOPI session: {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting session from store: {SessionId}", sessionId);
                }
            }

            _logger.LogInformation("Completed batch deletion of {Count} sessions", sessionsToDelete.Count);
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
                    _logger.LogInformation($"Downloading file from: {session.FileUrl}");
                    
                    // Stream download like JavaScript implementation
                    using var response = await _httpClient.GetAsync(session.FileUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(session.LocalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                    
                    await contentStream.CopyToAsync(fileStream);
                    
                    session.Downloaded = true;
                    _logger.LogInformation("File downloaded successfully");
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
                _logger.LogInformation("No upload URL configured for session: {SessionId}", sessionId);
                return null;
            }

            try
            {
                _logger.LogInformation($"Uploading file to: {session.UploadUrl}");
                
                var uploadHeaders = JsonConvert.DeserializeObject<Dictionary<string, string>>(session.UploadHeaders ?? "{}");
                
                using var content = new ByteArrayContent(fileBuffer);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                
                using var request = new HttpRequestMessage(HttpMethod.Post, session.UploadUrl)
                {
                    Content = content
                };
                
                // Add custom headers
                foreach (var header in uploadHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("File uploaded successfully, response status: {StatusCode}", response.StatusCode);
                var result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                throw;
            }
        }

        public async Task<(Stream fileStream, string fileName)> LockWopiFile(LockWopiFileCommand command)
        {
            var session = WopiSessionStore.Get(command.SessionId);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // No authentication check for lock operations (like JavaScript)
            _logger.LogInformation("Lock operation for session: {SessionId}, operation: {WopiOverride}", command.SessionId, command.WopiOverride);
            
            // Simple lock implementation (like JavaScript)
            if (command.WopiOverride != "LOCK" && command.WopiOverride != "UNLOCK" && command.WopiOverride != "REFRESH_LOCK")
            {
                throw new InvalidOperationException("Unsupported operation");
            }

            // For lock operations, we don't need to return file content
            // Just ensure the file exists and return success
            await EnsureFileExists(command.SessionId);

            _logger.LogInformation("Lock operation completed for session: {SessionId}, operation: {WopiOverride}", command.SessionId, command.WopiOverride);

            // Return empty stream for lock operations (they don't need file content)
            var emptyStream = new MemoryStream();
            return (emptyStream, session.FileName);
        }

        public async Task<bool> UploadFileToUrl(UploadFileToUrlCommand command, CancellationToken cancellationToken = default)
        {
            var session = WopiSessionStore.Get(command.SessionId);
            if (session == null)
            {
                _logger.LogInformation("Session not found: {SessionId}", command.SessionId);
                return false;
            }

            if (string.IsNullOrEmpty(command.UploadUrl))
            {
                _logger.LogInformation("No upload URL provided for session: {SessionId}", command.SessionId);
                return false;
            }

            try
            {
                _logger.LogInformation("Uploading file to: {UploadUrl}", command.UploadUrl);

                // Ensure the file exists before uploading
                await EnsureFileExists(command.SessionId);

                // Read the file content
                var fileBytes = await File.ReadAllBytesAsync(session.LocalFilePath);

                _logger.LogInformation("File Bytes length: {len}", fileBytes?.Length ?? 0);
                // Use the improved upload method
                return await UploadFileToStorageByUrlAsync(command.UploadUrl, fileBytes, command.UploadHeaders, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to {UploadUrl} for session {SessionId}", command.UploadUrl, command.SessionId);
                throw;
            }
        }

        public async Task<bool> UploadFileToStorageByUrlAsync(
            string uploadUrl,
            byte[] bytes,
            Dictionary<string, string> customHeaders = null,
            CancellationToken token = default)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = new ByteArrayContent(bytes)
            };

            // Add custom headers if provided
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    if (request.Headers.Contains(header.Key))
                    {
                        request.Headers.Remove(header.Key);
                    }
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var response = await client.SendAsync(
                                     request,
                                     HttpCompletionOption.ResponseHeadersRead,
                                     token).ConfigureAwait(false);

            var code = response.EnsureSuccessStatusCode().StatusCode;
            _logger.LogInformation("Blob upload → {StatusCode}", (int)code);

            return code is HttpStatusCode.OK or HttpStatusCode.Created;
        }
    }
} 