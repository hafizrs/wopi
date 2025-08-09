using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace Selise.Ecap.SC.Wopi.WebService
{
    [Route("wopi--")]
    [ApiController]
    public class SimpleWopiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimpleWopiController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _localFilePath;
        private static readonly Dictionary<string, WopiSessionData> _sessions = new();

        public SimpleWopiController(IConfiguration configuration, ILogger<SimpleWopiController> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            
            // Create temp_files directory like JavaScript version
            _localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configuration["LocalFilePath"] ?? "temp_files");
            if (!Directory.Exists(_localFilePath))
            {
                Directory.CreateDirectory(_localFilePath);
            }
        }

        [HttpPost("create-session")]
        public IActionResult CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                _logger.LogInformation("=== CREATE SESSION REQUEST ===");
                _logger.LogInformation("Headers: {Headers}", Request.Headers);
                _logger.LogInformation("Body: {@Body}", request);
                _logger.LogInformation("================================");

                _logger.LogInformation("Parsed fileUrl: {FileUrl}", request.FileUrl);
                _logger.LogInformation("fileUrl type: {Type}", request.FileUrl?.GetType());
                _logger.LogInformation("fileUrl exists: {Exists}", !string.IsNullOrEmpty(request.FileUrl));

                if (string.IsNullOrEmpty(request.FileUrl))
                {
                    _logger.LogError("ERROR: fileUrl is missing or empty");
                    return BadRequest(new 
                    { 
                        error = "fileUrl is required",
                        received = request,
                        fileUrlValue = request.FileUrl
                    });
                }

                // Generate unique session ID like JavaScript crypto.randomUUID()
                var sessionId = Guid.NewGuid().ToString();
                var collaboraBaseUrl = _configuration["CollaboraBaseUrl"] ?? "https://colabora.rashed.app";
                var accessToken = request.AccessToken ?? _configuration["DefaultAccessToken"] ?? "default-token-123";

                var session = new WopiSessionData
                {
                    SessionId = sessionId,
                    FileUrl = request.FileUrl,
                    UploadUrl = request.UploadUrl,
                    UploadHeaders = request.UploadHeaders ?? new Dictionary<string, string>(),
                    FileName = request.FileName ?? _configuration["DefaultFileName"] ?? "document.docx",
                    AccessToken = accessToken,
                    UserId = request.UserId ?? "user1",
                    UserDisplayName = request.UserDisplayName ?? _configuration["DefaultUserDisplayName"] ?? "Anonymous User",
                    CanEdit = request.CanEdit,
                    CreatedAt = DateTime.UtcNow,
                    Downloaded = false
                };

                _sessions[sessionId] = session;

                _logger.LogInformation("Created session {SessionId} for file: {FileUrl}", sessionId, request.FileUrl);

                // Generate edit URL exactly like JavaScript
                var wopiSrc = Uri.EscapeDataString($"{collaboraBaseUrl}/wopi/files/{sessionId}");
                var editUrl = $"{collaboraBaseUrl}/browser/a8848448cc/cool.html?WOPISrc={wopiSrc}&access_token={accessToken}";

                return Ok(new
                {
                    sessionId,
                    editUrl,
                    wopiSrc = $"{collaboraBaseUrl}/wopi/files/{sessionId}",
                    accessToken,
                    message = "Session created successfully"
                });
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Error creating session");
                return StatusCode(500, new { error = "Failed to create session" });
            }
        }

        [HttpGet("sessions")]
        public IActionResult GetSessions()
        {
            var sessions = new List<object>();
            foreach (var kvp in _sessions)
            {
                sessions.Add(new
                {
                    SessionId = kvp.Key,
                    FileName = kvp.Value.FileName,
                    UserId = kvp.Value.UserId,
                    CanEdit = kvp.Value.CanEdit,
                    CreatedAt = kvp.Value.CreatedAt
                });
            }
            return Ok(sessions);
        }

        [HttpGet("session/{sessionId}")]
        public IActionResult GetSession(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return NotFound(new { error = "Session not found" });
            }

            return Ok(new
            {
                SessionId = sessionId,
                FileName = session.FileName,
                FileUrl = session.FileUrl,
                UploadUrl = session.UploadUrl,
                UserId = session.UserId,
                UserDisplayName = session.UserDisplayName,
                CanEdit = session.CanEdit,
                Downloaded = session.Downloaded,
                CreatedAt = session.CreatedAt
            });
        }

        [HttpDelete("session/{sessionId}")]
        public IActionResult DeleteSession(string sessionId)
        {
            if (_sessions.ContainsKey(sessionId))
            {
                // Delete local file if exists like JavaScript version
                var filePath = Path.Combine(_localFilePath, $"{sessionId}.docx");
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _sessions.Remove(sessionId);
                return Ok(new { message = "Session cleaned up successfully" });
            }
            else
            {
                return NotFound(new { error = "Session not found" });
            }
        }

        [HttpGet("files/{sessionId}")]
        public async Task<IActionResult> CheckFileInfo(string sessionId)
        {
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    _logger.LogWarning("Session not found: {SessionId}", sessionId);
                    return NotFound(new { error = "Session not found" });
                }

                // Check for authentication like JavaScript version
                var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "") ?? 
                           Request.Query["access_token"].FirstOrDefault();
                
                _logger.LogInformation("CheckFileInfo - Token received: {Token}", token);

                if (string.IsNullOrEmpty(token) || token != session.AccessToken)
                {
                    _logger.LogWarning("CheckFileInfo - Authentication failed");
                    return Unauthorized(new { error = "Unauthorized" });
                }

                // Ensure file exists locally like JavaScript version
                var filePath = await EnsureFileExists(sessionId);
                var fileInfo = new FileInfo(filePath);

                _logger.LogInformation("CheckFileInfo - Success for session: {SessionId}", sessionId);

                return Ok(new
                {
                    BaseFileName = session.FileName,
                    Size = fileInfo.Length,
                    OwnerId = session.UserId,
                    UserId = session.UserId,
                    UserCanWrite = session.CanEdit,
                    UserCanRename = false,
                    UserCanNotWriteRelative = false,
                    Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    UserFriendlyName = session.UserDisplayName,
                    PostMessageOrigin = _configuration["CollaboraBaseUrl"] ?? "https://colabora.rashed.app",
                    // Additional permissions for editing like JavaScript
                    EnableOwnerTermination = false,
                    SupportsLocks = true,
                    SupportsGetLock = true,
                    SupportsExtendedLockLength = true,
                    SupportsCobalt = false,
                    SupportsUpdate = session.CanEdit,
                    UserCanPresent = false
                });
            }
            catch (Exception error)
            {
                _logger.LogError(error, "CheckFileInfo - Error");
                return StatusCode(500, new { error = "Failed to get file info" });
            }
        }

        [HttpGet("files/{sessionId}/contents")]
        public async Task<IActionResult> GetFile(string sessionId)
        {
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    return NotFound("Session not found");
                }

                // Check for authentication like JavaScript version
                var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "") ?? 
                           Request.Query["access_token"].FirstOrDefault();
                
                _logger.LogInformation("GetFile - Token received: {Token}", token);

                if (string.IsNullOrEmpty(token) || token != session.AccessToken)
                {
                    _logger.LogWarning("GetFile - Authentication failed");
                    return Unauthorized("Unauthorized");
                }

                // Get file from local storage like JavaScript version
                var filePath = await EnsureFileExists(sessionId);
                
                _logger.LogInformation("GetFile - Streaming file for session: {SessionId}", sessionId);
                
                var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            }
            catch (Exception error)
            {
                _logger.LogError(error, "GetFile - Error");
                return StatusCode(500, "Error streaming file content");
            }
        }

        [HttpPost("files/{sessionId}/contents")]
        public async Task<IActionResult> PutFile(string sessionId)
        {
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    return NotFound("Session not found");
                }

                // Check for authentication like JavaScript version
                var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "") ?? 
                           Request.Query["access_token"].FirstOrDefault();
                
                _logger.LogInformation("PutFile - Token received: {Token}", token);

                if (string.IsNullOrEmpty(token) || token != session.AccessToken)
                {
                    _logger.LogWarning("PutFile - Authentication failed");
                    return Unauthorized("Unauthorized");
                }

                if (!session.CanEdit)
                {
                    _logger.LogWarning("PutFile - Edit not allowed for this session");
                    return StatusCode(403, "Edit not allowed");
                }

                // Read the file content from request body like JavaScript version
                using var memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);
                var fileBuffer = memoryStream.ToArray();

                var filePath = Path.Combine(_localFilePath, $"{sessionId}.docx");

                _logger.LogInformation("PutFile - Saving file for session: {SessionId}", sessionId);
                _logger.LogInformation("PutFile - File size: {Size}", fileBuffer.Length);

                // Save the updated file locally like JavaScript version
                await System.IO.File.WriteAllBytesAsync(filePath, fileBuffer);

                // Upload to external service if configured like JavaScript version
                object uploadResult = null;
                if (!string.IsNullOrEmpty(session.UploadUrl))
                {
                    try
                    {
                        uploadResult = await UploadFile(sessionId, fileBuffer);
                        _logger.LogInformation("PutFile - File uploaded to external service");
                    }
                    catch (Exception uploadError)
                    {
                        _logger.LogError(uploadError, "PutFile - Upload failed, but file saved locally");
                        // Continue even if upload fails like JavaScript version
                    }
                }

                _logger.LogInformation("PutFile - File saved successfully");

                // Return success response like JavaScript version
                return Ok(new
                {
                    LastModifiedTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Name = session.FileName,
                    Size = fileBuffer.Length,
                    Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    UploadResult = uploadResult != null ? "Success" : (!string.IsNullOrEmpty(session.UploadUrl) ? "Failed" : "Not configured")
                });
            }
            catch (Exception error)
            {
                _logger.LogError(error, "PutFile - Error");
                return StatusCode(500, "Error saving file");
            }
        }

        [HttpPost("files/{sessionId}")]
        public IActionResult Lock(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return NotFound("Session not found");
            }

            // Check access token like JavaScript version
            var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "") ?? 
                       Request.Query["access_token"].FirstOrDefault();

            if (string.IsNullOrEmpty(token) || token != session.AccessToken)
            {
                return Unauthorized();
            }

            var wopiOverride = Request.Headers["x-wopi-override"].FirstOrDefault();
            _logger.LogInformation("Lock operation for session: {SessionId}, operation: {Operation}", sessionId, wopiOverride);

            // Simple lock implementation like JavaScript version
            if (wopiOverride == "LOCK" || wopiOverride == "UNLOCK" || wopiOverride == "REFRESH_LOCK")
            {
                return Ok(new
                {
                    Name = session.FileName,
                    Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                });
            }
            else
            {
                return BadRequest("Unsupported operation");
            }
        }

        // Download file from URL to local storage (like JavaScript ensureFileExists)
        private async Task<string> EnsureFileExists(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException("Session not found");
            }

            var filePath = Path.Combine(_localFilePath, $"{sessionId}.docx");

            if (!session.Downloaded)
            {
                _logger.LogInformation("Downloading file from: {FileUrl}", session.FileUrl);
                try
                {
                    using var response = await _httpClient.GetAsync(session.FileUrl);
                    response.EnsureSuccessStatusCode();
                    
                    var fileContent = await response.Content.ReadAsByteArrayAsync();
                    await System.IO.File.WriteAllBytesAsync(filePath, fileContent);
                    
                    session.Downloaded = true;
                    _logger.LogInformation("File downloaded successfully");
                }
                catch (Exception error)
                {
                    _logger.LogError(error, "Error downloading file");
                    throw;
                }
            }

            return filePath;
        }

        // Upload file to external service (like JavaScript uploadFile)
        private async Task<object> UploadFile(string sessionId, byte[] fileBuffer)
        {
            if (!_sessions.TryGetValue(sessionId, out var session) || string.IsNullOrEmpty(session.UploadUrl))
            {
                _logger.LogInformation("No upload URL configured for session: {SessionId}", sessionId);
                return null;
            }

            try
            {
                _logger.LogInformation("Uploading file to: {UploadUrl}", session.UploadUrl);

                using var content = new ByteArrayContent(fileBuffer);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                
                // Add upload headers like JavaScript version
                foreach (var header in session.UploadHeaders)
                {
                    content.Headers.Add(header.Key, header.Value);
                }

                using var response = await _httpClient.PostAsync(session.UploadUrl, content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("File uploaded successfully, response status: {StatusCode}", response.StatusCode);
                
                var result = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(result);
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Error uploading file");
                throw;
            }
        }
    }

    // Data classes matching JavaScript functionality
    public class CreateSessionRequest
    {
        public string FileUrl { get; set; }
        public string UploadUrl { get; set; }
        public Dictionary<string, string> UploadHeaders { get; set; } = new();
        public string FileName { get; set; }
        public string AccessToken { get; set; } = "default-token-123";
        public string UserId { get; set; } = "user1";
        public string UserDisplayName { get; set; } = "Anonymous User";
        public bool CanEdit { get; set; } = true;
    }

    public class WopiSessionData
    {
        public string SessionId { get; set; }
        public string FileUrl { get; set; }
        public string UploadUrl { get; set; }
        public Dictionary<string, string> UploadHeaders { get; set; } = new();
        public string FileName { get; set; }
        public string AccessToken { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public bool CanEdit { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Downloaded { get; set; }
    }
}
