using Aspose.Pdf.Operators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _sessionLocks = new ConcurrentDictionary<string, SemaphoreSlim>(); // Session-level locks
        private const int BUFFER_SIZE = 64 * 1024; // 64KB buffer for streaming operations
        private const int LOCK_TIMEOUT_SECONDS = 300; // 5 minute timeout for lock operations (for large file uploads)

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
            
            _logger.LogInformation("WopiService initialized with async session storage and streaming operations");
        }

        // Helper method to get or create session-specific locks
        private SemaphoreSlim GetOrCreateSessionLock(string sessionId)
        {
            return _sessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        }

        // Method to get lock status for monitoring (useful for debugging)
        public Dictionary<string, bool> GetSessionLockStatus()
        {
            var status = new Dictionary<string, bool>();
            foreach (var kvp in _sessionLocks)
            {
                status[kvp.Key] = kvp.Value.CurrentCount == 0; // true = locked, false = available
            }
            return status;
        }

        // Get current lock timeout configuration
        public int GetLockTimeoutSeconds() => LOCK_TIMEOUT_SECONDS;

        // Implement IDisposable to properly dispose of all session locks
        public void Dispose()
        {
            foreach (var sessionLock in _sessionLocks.Values)
            {
                sessionLock?.Dispose();
            }
            _sessionLocks.Clear();
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
                UserId = "anonymous",           // Hide real user identity
                UserDisplayName = "User",       // Hide real user name
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
                UserCanRename = false,          // Hide rename button
                UserCanNotWriteRelative = true, // Prevent "Save As" functionality
                Version = DateTime.UtcNow.Ticks.ToString(), // CRITICAL: Required for X-WOPI-ItemVersion header
                UserFriendlyName = "User",      // Hide real user name
                PostMessageOrigin = _collaboraBaseUrl,
                
                // HIDE UNNECESSARY FEATURES:
                EnableOwnerTermination = false,
                SupportsLocks = false,           // Hide lock operations
                SupportsGetLock = false,         // Hide lock UI
                SupportsExtendedLockLength = false,
                SupportsCobalt = false,          // Hide advanced features
                SupportsUpdate = true,           // Keep file update/save
                UserCanPresent = false,         // Hide presentation mode
                
                // CRITICAL: Set properties to hide UI elements
                SupportsPutFile = true,          // Keep file save functionality
                SupportsUnlock = true,          // Hide unlock
                SupportsRefreshLock = true,     // Hide lock refresh
                SupportsGetFile = true,          // Keep file retrieval
                SupportsCheckFileInfo = true,    // Keep file info
                SupportsDeleteFile = false,      // Hide delete button
                SupportsRenameFile = false,      // Hide rename button
                SupportsPutRelativeFile = false, // Hide "File" tab and "Save As" button
                SupportsGetFileWopiSrc = false,  // Hide blue diamond icon (file source)
                SupportsExecuteCobaltRequest = false,
                SupportsUserInfo = false,        // Hide user info (prevents multi-user annotations)
                SupportsFolders = false,         // Hide folder operations
                SupportsFileCreation = false,    // Hide document icon (file creation)
                
                // Additional critical properties for Collabora
                AllowWrite = session.CanEdit,    // CRITICAL: Must match UserCanWrite
                
                // CUSTOM UI HIDING PROPERTIES (adopted from your request):
                HideFileMenu = true,             // Hide File menu completely
                HideHelpMenu = true,             // Hide Help menu
                HideToolsMenu = true,            // Hide Tools menu
                HideViewMenu = false,            // Keep View menu (needed for editing)
                HideUserList = true,             // Hide user list (prevents multi-user annotations)
                DisableCopy = false,             // Keep copy functionality
                DisablePrint = true,             // Hide print option
                DisableExport = true,            // Hide export option
                DisableSave = false,             // Keep save functionality
                EnableShare = false,             // Hide share functionality
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
            return new FileStream(session.LocalFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
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
                // Save the updated file locally using async lock
                await FileReadWriteInLockAsync(session.LocalFilePath, command.FileContent, true, CancellationToken.None);
                _logger.LogInformation("PutFile - File saved locally for session: {SessionId}", command.SessionId);
                
                // Upload to external service if configured
                bool? uploadResult = null;
                string uploadStatus = "Not configured";

                try
                {
                    uploadResult = await UploadFile(command.SessionId, command.FileContent);
                    if (uploadResult == true)
                    {
                        uploadStatus = "Success";
                        _logger.LogInformation("PutFile - File uploaded to external service for session: {SessionId}", command.SessionId);
                    }
                    else if (uploadResult == false)
                    {
                        uploadStatus = "Failed";
                        _logger.LogError("PutFile - Upload failed for session: {SessionId}, but file saved locally", command.SessionId);
                    }
                }
                catch (Exception ex)
                {
                    uploadStatus = "Failed";
                    _logger.LogError(ex, "PutFile - Upload failed for session: {SessionId}, but file saved locally", command.SessionId);
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

            // Batch delete from session store and clean up locks
            foreach (var sessionId in sessionsToDelete)
            {
                try
                {
                    WopiSessionStore.Delete(sessionId);
                    //await _repository.DeleteAsync<WopiSession>(s => s.SessionId == sessionId);
                    _logger.LogDebug("Deleted WOPI session: {SessionId}", sessionId);
                    
                    // Clean up session lock
                    if (_sessionLocks.TryRemove(sessionId, out var sessionLock))
                    {
                        sessionLock?.Dispose();
                        _logger.LogDebug("Cleaned up lock for deleted session: {SessionId}", sessionId);
                    }
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
                    using var fileStream = new FileStream(session.LocalFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192, true);
                    
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

        private async Task<byte[]> FileReadWriteInLockAsync(string localFilePath, byte[] fileBytes, bool isWrite, CancellationToken cancellationToken = default)
        {
            // Extract session ID from file path for session-level locking
            var sessionId = Path.GetFileNameWithoutExtension(localFilePath);
            var sessionLock = GetOrCreateSessionLock(sessionId);
            
            // Try to acquire lock with timeout
            if (!await sessionLock.WaitAsync(TimeSpan.FromSeconds(LOCK_TIMEOUT_SECONDS), cancellationToken))
            {
                throw new TimeoutException($"Operation timed out waiting for lock on session {sessionId}. Lock timeout: {LOCK_TIMEOUT_SECONDS} seconds (5 minutes).");
            }
            
            try
            {
                if (isWrite)
                {
                    await File.WriteAllBytesAsync(localFilePath, fileBytes, cancellationToken);
                    return null;
                }
                else
                {
                    // Use streaming for large files to avoid memory issues
                    using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var memoryStream = new MemoryStream();
                    
                    var buffer = new byte[BUFFER_SIZE];
                    int bytesRead;
                    
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    }
                    
                    return memoryStream.ToArray();
                }
            }
            finally
            {
                sessionLock.Release();
            }
        }

        private async Task<bool?> UploadFile(string sessionId, byte[] fileBuffer, Dictionary<string, string> uploadHeaders = null)
        {
            await Task.Delay(100);
            var session = WopiSessionStore.Get(sessionId);
            if (session == null || string.IsNullOrEmpty(session.UploadUrl))
            {
                _logger.LogInformation("No upload URL configured for session: {SessionId}", sessionId);
                return null;
            }

            try
            {
                _logger.LogInformation($"Uploading file to: {session.UploadUrl}");
                
                uploadHeaders ??= JsonConvert.DeserializeObject<Dictionary<string, string>>(session.UploadHeaders ?? "{}");

                return await UploadFileToStorageByUrlAsync(session.UploadUrl, fileBuffer, uploadHeaders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return false;
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

                // Update the session's UploadUrl and UploadHeaders for large file handling
                // This ensures that if PutFile is called later, it will use the latest upload URL
                var originalUploadUrl = session.UploadUrl;
                session.UploadUrl = command.UploadUrl;  // Always update with the new upload URL
                session.UploadHeaders = JsonConvert.SerializeObject(command.UploadHeaders ?? new Dictionary<string, string>());
                session.LastUpdateDate = DateTime.UtcNow.ToLocalTime();

                // Update both in-memory and disk storage
                WopiSessionStore.Set(command.SessionId, session);
                
                _logger.LogInformation("Updated session {SessionId} UploadUrl from {OldUrl} to {NewUrl}", 
                    command.SessionId, originalUploadUrl, command.UploadUrl);

                // Ensure the file exists before uploading
                await EnsureFileExists(command.SessionId);

                // Read the file content using async lock
                var fileBytes = await FileReadWriteInLockAsync(session.LocalFilePath, null, false, cancellationToken);

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
            
            // Use streaming content for better memory management with large files
            using var streamContent = new StreamContent(new MemoryStream(bytes));
            using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = streamContent
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