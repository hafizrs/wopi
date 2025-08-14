using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using Selise.Ecap.SC.Wopi.Contracts.Models;
using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.Queries.WopiModule;
using Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.WebService
{
    [Route("wopi")]
    [ApiController]
    public class WopiController : ControllerBase
    {
        private readonly ILogger<WopiController> _logger;
        private readonly IWopiService _service;

        public WopiController(
            IWopiService service,
            ILogger<WopiController> logger)
        {
            _logger = logger;
            _service = service;
        }

        // Handle OPTIONS requests for CORS preflight
        [HttpOptions("files/{sessionId}")]
        [HttpOptions("files/{sessionId}/contents")]
        [AllowAnonymous]
        public IActionResult HandleOptions(string sessionId)
        {
            _logger.LogInformation("OPTIONS request received for session: {SessionId}", sessionId);
            
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-WOPI-Override, X-WOPI-Lock, X-WOPI-LockExpires, X-WOPI-ItemVersion, X-WOPI-ServerError, X-WOPI-ServerVersion");
            Response.Headers.Add("Access-Control-Max-Age", "86400");
            
            return Ok();
        }

        // Test endpoint to verify service is working
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            _logger.LogInformation("Test endpoint called");
            return Ok(new { 
                Message = "WOPI Service is running", 
                Timestamp = DateTime.UtcNow,
                Endpoints = new[] {
                    "GET /wopi - WOPI Discovery",
                    "POST /wopi/create-session - Create Session",
                    "GET /wopi/files/{sessionId} - Check File Info",
                    "GET /wopi/files/{sessionId}/contents - Get File",
                    "POST /wopi/files/{sessionId}/contents - Put File",
                    "POST /wopi/files/{sessionId} - Lock/Unlock",
                    "GET /wopi/sessions - Get All Sessions"
                }
            });
        }

        // WOPI Discovery endpoint
        [HttpGet("")]
        [AllowAnonymous]
        public IActionResult WopiDiscovery()
        {
            _logger.LogInformation("=== WOPI DISCOVERY CALLED ===");
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            _logger.LogInformation("Base URL: {BaseUrl}", baseUrl);
            
            // Standard WOPI discovery format that Collabora expects
            var discovery = new
            {
                net_zone = "external-http",
                app = new
                {
                    name = "WOPI Host",
                    favIconUrl = $"{baseUrl}/favicon.ico",
                    checkLicense = false,
                    hasTheme = false,
                    // CRITICAL: These must be exactly as Collabora expects
                    supportsGetLock = true,
                    supportsLocks = true,
                    supportsExtendedLockLength = true,
                    supportsFileCreation = false,
                    supportsRename = false,
                    supportsDeleteFile = false,
                    supportsUserInfo = false,
                    supportsFolders = false,
                    supportsUpdate = true,
                    supportsCobalt = false,
                    supportsGetFileWopiSrc = false,
                    supportsPutFile = true,
                    supportsPutRelativeFile = true,
                    supportsUnlock = true,
                    supportsRefreshLock = true,
                    supportsLock = true,
                    supportsGetFile = true,
                    supportsCheckFileInfo = true,
                    supportsExecuteCobaltRequest = false
                }
            };

            _logger.LogInformation("WOPI Discovery endpoint called, returning capabilities: {Capabilities}", 
                System.Text.Json.JsonSerializer.Serialize(discovery));

            return Ok(discovery);
        }

        // Session Management Endpoints (like JavaScript implementation)
        [HttpPost("create-session")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSession([FromBody] CreateWopiSessionCommand command)
        {
            if (command == null) return null;

            return Ok(await _service.CreateWopiSession(command));
        }

        [HttpGet("sessions")]
        [AllowAnonymous]
        public QueryHandlerResponse GetAllSessions()
        {
            var query = new GetWopiSessionsQuery();
            return new QueryHandlerResponse() {
                Data = _service.GetWopiSessions(query)
            };
        }

        [HttpGet("session/{sessionId}")]
        [AllowAnonymous]
        public WopiSessionResponse GetSessionInfo(string sessionId)
        {
            var query = new GetWopiSessionQuery { SessionId = sessionId };
            return _service.GetWopiSession(query);
        }

        [HttpDelete("session")]
        [AllowAnonymous]
        public async Task CleanupSessions([FromBody] string[] sessionIds)
        {
            var command = new DeleteWopiSessionCommand { SessionIds = sessionIds };
            await _service.DeleteWopiSession(command);
        }

        [HttpDelete("session/{sessionId}")]
        [AllowAnonymous]
        public async Task CleanupSingleSession(string sessionId)
        {
            var command = new DeleteWopiSessionCommand { SessionIds = new[] { sessionId } };
            await _service.DeleteWopiSession(command);
        }

        // WOPI Protocol Endpoints (following standard WOPI specification)
        [HttpGet("files/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckFileInfo(string sessionId)
        {
            try
            {
                _logger.LogInformation("CheckFileInfo called for session: {SessionId}", sessionId);
                
                // Get access token from query or header
                var accessToken = Request.Query["access_token"].FirstOrDefault() ?? 
                                Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                var query = new GetWopiFileInfoQuery 
                { 
                    SessionId = sessionId,
                    AccessToken = accessToken
                };

                var result = await _service.GetWopiFileInfo(query);
                
                // CRITICAL: Add ALL required WOPI headers that Collabora expects
                Response.Headers.Add("X-WOPI-ItemVersion", result.Version);
                Response.Headers.Add("X-WOPI-Lock", Guid.NewGuid().ToString());
                Response.Headers.Add("X-WOPI-LockExpires", DateTime.UtcNow.AddMinutes(30).ToString("R"));
                Response.Headers.Add("X-WOPI-ServerError", "");
                Response.Headers.Add("X-WOPI-ServerVersion", "1.0");
                
                // Add CORS headers for WOPI
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-WOPI-Override, X-WOPI-Lock, X-WOPI-LockExpires");
                
                _logger.LogInformation("CheckFileInfo - Success for session: {SessionId}, UserCanWrite: {UserCanWrite}, SupportsPutFile: {SupportsPutFile}", 
                    sessionId, result.UserCanWrite, result.SupportsPutFile);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("CheckFileInfo - Unauthorized for session: {SessionId}", sessionId);
                return Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                _logger.LogWarning("CheckFileInfo - Session not found: {SessionId}", sessionId);
                return NotFound("Session not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckFileInfo for session: {SessionId}", sessionId);
                return StatusCode(500);
            }
        }

        [HttpGet("files/{sessionId}/contents")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFile(string sessionId)
        {
            try
            {
                // Get access token from query or header
                var accessToken = Request.Query["access_token"].FirstOrDefault() ?? 
                                Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                var query = new GetWopiFileContentQuery 
                { 
                    SessionId = sessionId,
                    AccessToken = accessToken
                };

                var result = await _service.GetWopiFileContent(query);
                
                if (result != null)
                {
                    // Add required WOPI headers
                    Response.Headers.Add("X-WOPI-ItemVersion", DateTime.UtcNow.Ticks.ToString());
                    Response.Headers.Add("X-WOPI-Lock", Guid.NewGuid().ToString());
                    Response.Headers.Add("X-WOPI-LockExpires", DateTime.UtcNow.AddMinutes(30).ToString("R"));
                    
                    return File(result, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                }
                else
                {
                    return StatusCode(500);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                return NotFound("Session not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFile");
                return StatusCode(500);
            }
        }

        [HttpPost("files/{sessionId}/contents")]
        [AllowAnonymous]
        public async Task<IActionResult> PutFile(string sessionId)
        {
            try
            {
                _logger.LogInformation("=== PUTFILE CALLED ===");
                _logger.LogInformation("PutFile called for session: {SessionId}", sessionId);
                _logger.LogInformation("Request Method: {Method}", Request.Method);
                _logger.LogInformation("Request Path: {Path}", Request.Path);
                _logger.LogInformation("Request QueryString: {QueryString}", Request.QueryString);
                _logger.LogInformation("Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Content-Length: {ContentLength}", Request.ContentLength);
                
                // Get access token from query or header
                var accessToken = Request.Query["access_token"].FirstOrDefault() ?? 
                                Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                _logger.LogInformation("Access Token: {AccessToken}", accessToken);

                // Log all headers for debugging
                foreach (var header in Request.Headers)
                {
                    _logger.LogInformation("Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                }

                // Read the file content from request body
                using var memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                _logger.LogInformation("PutFile - File content size: {Size} bytes", fileContent.Length);

                var command = new UpdateWopiFileCommand 
                { 
                    SessionId = sessionId,
                    AccessToken = accessToken,
                    FileContent = fileContent
                };

                var result = await _service.UpdateWopiFile(command);
                
                if (result != null)
                {
                    // Add required WOPI headers
                    Response.Headers.Add("X-WOPI-ItemVersion", result.Version);
                    Response.Headers.Add("X-WOPI-Lock", Guid.NewGuid().ToString());
                    Response.Headers.Add("X-WOPI-LockExpires", DateTime.UtcNow.AddMinutes(30).ToString("R"));
                    
                    _logger.LogInformation("PutFile completed successfully for session: {SessionId}", sessionId);
                    return Ok(result);
                }
                else
                {
                    _logger.LogError("PutFile - Service returned null result for session: {SessionId}", sessionId);
                    return StatusCode(500);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("PutFile unauthorized for session: {SessionId}", sessionId);
                return Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                _logger.LogWarning("PutFile session not found: {SessionId}", sessionId);
                return NotFound("Session not found");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Edit not allowed"))
            {
                _logger.LogWarning("PutFile edit not allowed for session: {SessionId}", sessionId);
                return StatusCode(403);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PutFile for session: {SessionId}", sessionId);
                return StatusCode(500);
            }
        }

        [HttpPost("files/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> Lock(string sessionId)
        {
            try
            {
                // Get access token from query or header
                var accessToken = Request.Query["access_token"].FirstOrDefault() ?? 
                                Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                var wopiOverride = Request.Headers["x-wopi-override"].FirstOrDefault();

                _logger.LogInformation("Lock operation for session: {SessionId}, operation: {WopiOverride}", sessionId, wopiOverride);

                // Validate the operation
                if (string.IsNullOrEmpty(wopiOverride))
                {
                    _logger.LogWarning("Lock operation missing x-wopi-override header for session: {SessionId}", sessionId);
                    return BadRequest("Missing x-wopi-override header");
                }

                // Check if session exists and can be edited
                var session = _service.GetWopiSession(new GetWopiSessionQuery { SessionId = sessionId });
                if (session == null)
                {
                    _logger.LogWarning("Lock operation - Session not found: {SessionId}", sessionId);
                    return NotFound("Session not found");
                }

                // Generate lock ID and expiration
                var lockId = Guid.NewGuid().ToString();
                var lockExpires = DateTime.UtcNow.AddMinutes(30);

                // Add required WOPI headers
                Response.Headers.Add("X-WOPI-ItemVersion", DateTime.UtcNow.Ticks.ToString());
                Response.Headers.Add("X-WOPI-Lock", lockId);
                Response.Headers.Add("X-WOPI-LockExpires", lockExpires.ToString("R"));
                
                // For lock operations, return success status
                if (wopiOverride == "LOCK")
                {
                    _logger.LogInformation("Lock operation completed successfully for session: {SessionId}, LockId: {LockId}", sessionId, lockId);
                    // Return proper WOPI response format
                    return Ok(new { 
                        Name = session.FileName,
                        Version = DateTime.UtcNow.Ticks.ToString()
                    });
                }
                else if (wopiOverride == "UNLOCK")
                {
                    _logger.LogInformation("Unlock operation completed successfully for session: {SessionId}", sessionId);
                    // Return proper WOPI response format
                    return Ok(new { 
                        Name = session.FileName,
                        Version = DateTime.UtcNow.Ticks.ToString()
                    });
                }
                else if (wopiOverride == "REFRESH_LOCK")
                {
                    _logger.LogInformation("Refresh lock operation completed successfully for session: {SessionId}, LockId: {LockId}", sessionId, lockId);
                    // Return proper WOPI response format
                    return Ok(new { 
                        Name = session.FileName,
                        Version = DateTime.UtcNow.Ticks.ToString()
                    });
                }
                else
                {
                    _logger.LogWarning("Lock operation - Unsupported operation: {WopiOverride} for session: {SessionId}", wopiOverride, sessionId);
                    return BadRequest("Unsupported lock operation");
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                return NotFound("Session not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Lock operation for session: {SessionId}", sessionId);
                return StatusCode(500);
            }
        }

        // Upload file to external URL endpoint
        [HttpPost("uploadToUrl")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadFileToUrl([FromBody] UploadFileToUrlCommand command)
        {
            try
            {
                if (command == null)
                {
                    return BadRequest("Command cannot be null");
                }

                if (string.IsNullOrEmpty(command.SessionId))
                {
                    return BadRequest("SessionId is required");
                }

                var success = await _service.UploadFileToUrl(command);
                
                if (success)
                {
                    return Ok(success);
                }
                else
                {
                    return NotFound("Session not found or no upload URL provided");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to URL for session: {SessionId}", command?.SessionId ?? "unknown");
                return StatusCode(500, "Error uploading file");
            }
        }

        private CommandResponse ErrorResponse()
        {
            var response = new CommandResponse();
            response.SetError("Command", "Invalid value");
            return response;
        }
    }
}