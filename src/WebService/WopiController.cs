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

        // Session Management Endpoints (like JavaScript implementation)
        [HttpPost("create-session")]
        [AllowAnonymous]
        public async Task<CreateWopiSessionResponse> CreateSession([FromBody] CreateWopiSessionCommand command)
        {
            if (command == null) return null;

            return await _service.CreateWopiSession(command);
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
        public async Task<IResult> CheckFileInfo(string sessionId)
        {
            try
            {
                // Get access token from query or header
                var accessToken = Request.Query["access_token"].FirstOrDefault() ?? 
                                Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                var query = new GetWopiFileInfoQuery 
                { 
                    SessionId = sessionId,
                    AccessToken = accessToken
                };

                var result = await _service.GetWopiFileInfo(query);
                return Results.Json(result);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                return Results.NotFound("Session not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckFileInfo");
                return Results.StatusCode(500);
            }
        }

        [HttpGet("files/{sessionId}/contents")]
        [AllowAnonymous]
        public async Task<IResult> GetFile(string sessionId)
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
                    return Results.File(result, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                }
                else
                {
                    return Results.StatusCode(500);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                return Results.NotFound("Session not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFile");
                return Results.StatusCode(500);
            }
        }

        [HttpPost("files/{sessionId}/contents")]
        [AllowAnonymous]
        public async Task<IResult> PutFile(string sessionId)
        {
            try
            {
                // Get access token from query or header
                var accessToken = Request.Query["access_token"].FirstOrDefault() ?? 
                                Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                // Read the file content from request body
                using var memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                var command = new UpdateWopiFileCommand 
                { 
                    SessionId = sessionId,
                    AccessToken = accessToken,
                    FileContent = fileContent
                };

                var result = await _service.UpdateWopiFile(command); ;
                
                if (result != null)
                {
                    return Results.Json(result);
                }
                else
                {
                    return Results.StatusCode(500);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                return Results.NotFound("Session not found");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Edit not allowed"))
            {
                return Results.StatusCode(403);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PutFile");
                return Results.StatusCode(500);
            }
        }

        [HttpPost("files/{sessionId}")]
        [AllowAnonymous]
        public async Task<IResult> Lock(string sessionId)
        {
            try
            {
                // Get access token from query or header
                var accessToken = Request.Query["access_token"].FirstOrDefault() ?? 
                                Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

                var wopiOverride = Request.Headers["x-wopi-override"].FirstOrDefault();

                var command = new LockWopiFileCommand 
                { 
                    SessionId = sessionId,
                    AccessToken = accessToken,
                    WopiOverride = wopiOverride
                };

                var result = await _service.LockWopiFile(command);
                
                if (result.fileStream != null)
                {
                    return Results.Json(new
                    {
                        Name = result.fileName,
                        Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                    });
                }
                else
                {
                    return Results.StatusCode(500);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
            {
                return Results.NotFound("Session not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Lock operation");
                return Results.StatusCode(500);
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