using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.Wopi.Contracts.Models;
using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using Selise.Ecap.SC.Wopi.Contracts.Queries.WopiModule;
using SeliseBlocks.Genesis.Framework;
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
        private readonly CommandHandler _commandService;
        private readonly ValidationHandler _validationHandler;
        private readonly QueryHandler _queryHandler;
        private readonly IServiceClient _serviceClient;
        private readonly ILogger<WopiController> _logger;

        public WopiController(
            CommandHandler commandService,
            ValidationHandler validationHandler,
            QueryHandler queryHandler,
            IServiceClient serviceClient,
            ILogger<WopiController> logger)
        {
            _commandService = commandService;
            _validationHandler = validationHandler;
            _queryHandler = queryHandler;
            _serviceClient = serviceClient;
            _logger = logger;
        }

        // Session Management Endpoints (like JavaScript implementation)
        [HttpPost("create-session")]
        [AllowAnonymous]
        public async Task<CreateWopiSessionResponse> CreateSession([FromBody] CreateWopiSessionCommand command)
        {
            if (command == null) return null;

            var result = await _validationHandler.SubmitAsync<CreateWopiSessionCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateWopiSessionCommand, CreateWopiSessionResponse>(command);
            }

            return null;
        }

        [HttpGet("sessions")]
        [AllowAnonymous]
        public QueryHandlerResponse GetAllSessions()
        {
            var query = new GetWopiSessionsQuery();
            return _queryHandler.Submit<GetWopiSessionsQuery, QueryHandlerResponse>(query);
        }

        [HttpGet("session/{sessionId}")]
        [AllowAnonymous]
        public QueryHandlerResponse GetSessionInfo(string sessionId)
        {
            var query = new GetWopiSessionQuery { SessionId = sessionId };
            return _queryHandler.Submit<GetWopiSessionQuery, QueryHandlerResponse>(query);
        }

        [HttpDelete("session/{sessionId}")]
        [AllowAnonymous]
        public async Task<CommandResponse> CleanupSession(string sessionId)
        {
            var command = new DeleteWopiSessionCommand { SessionId = sessionId };
            return await _commandService.SubmitAsync<DeleteWopiSessionCommand, CommandResponse>(command);
        }

        // WOPI Protocol Endpoints (following standard WOPI specification)
        [HttpGet("files/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckFileInfo(string sessionId)
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

                var result = _queryHandler.Submit<GetWopiFileInfoQuery, QueryHandlerResponse>(query);
                
                if (result.StatusCode == 0)
                {
                    return Ok(result.Data);
                }
                else
                {
                    return StatusCode(500, result.ErrorMessage);
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
                _logger.LogError(ex, "Error in CheckFileInfo");
                return StatusCode(500, "Failed to get file info");
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

                var result = _queryHandler.Submit<GetWopiFileContentQuery, QueryHandlerResponse>(query);
                
                if (result.StatusCode == 0 && result.Data is byte[] fileContent)
                {
                    return File(fileContent, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                }
                else
                {
                    return StatusCode(500, result.ErrorMessage);
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
                return StatusCode(500, "Error streaming file content");
            }
        }

        [HttpPost("files/{sessionId}/contents")]
        [AllowAnonymous]
        public async Task<IActionResult> PutFile(string sessionId)
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

                var result = await _commandService.SubmitAsync<UpdateWopiFileCommand, CommandResponse>(command);
                
                if (result.StatusCode == 0)
                {
                    return Ok(new 
                    {
                        LastModifiedTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        Name = "Document.docx",
                        Size = fileContent.Length,
                        Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                    });
                }
                else
                {
                    return StatusCode(500, "Error saving file");
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("Edit not allowed"))
            {
                return StatusCode(403, "Edit not allowed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PutFile");
                return StatusCode(500, "Error saving file");
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

                var command = new LockWopiFileCommand 
                { 
                    SessionId = sessionId,
                    AccessToken = accessToken,
                    WopiOverride = wopiOverride
                };

                var result = await _commandService.SubmitAsync<LockWopiFileCommand, CommandResponse>(command);
                
                if (result.StatusCode == 0)
                {
                    return Ok(new 
                    {
                        Name = "Document.docx",
                        Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                    });
                }
                else
                {
                    return StatusCode(500, "Error in lock operation");
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
                _logger.LogError(ex, "Error in Lock operation");
                return StatusCode(500, ex.Message);
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