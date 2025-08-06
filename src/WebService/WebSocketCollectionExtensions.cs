using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.AIConversations;
using System;
using System.Linq;
using System.Net.WebSockets;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public static class WebSocketCollectionExtensions
    {
        public static void HandleAIWebSocketConnection(IApplicationBuilder websocketApp)
        {
            var loggerFactory = websocketApp.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("HandleAIWebSocketConnection");
            try
            {
                websocketApp.Use(async (context, next) =>
                {
                    try
                    {
                        // Handle WebSocket requests
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var requestedProtocols = context.WebSockets.WebSocketRequestedProtocols;

                            if (requestedProtocols.Count > 0)
                            {
                                var token = requestedProtocols.FirstOrDefault();
                                var isAuthenticated = context.User.Identity.IsAuthenticated;

                                if (!string.IsNullOrEmpty(token) && isAuthenticated)
                                {
                                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync(token);
                                    var handler = context.RequestServices.GetRequiredService<IAIWebSocketServerService>();
                                    await handler.HandleClientConnectionAsync(webSocket);
                                }
                                else
                                {
                                    // Invalid token or unauthenticated user
                                    if (!context.Response.HasStarted)
                                    {
                                        context.Response.StatusCode = 401; // Unauthorized
                                    }
                                    return;
                                }
                            }
                            else
                            {
                                // No subprotocol provided
                                if (!context.Response.HasStarted)
                                {
                                    context.Response.StatusCode = 400; // Bad Request
                                }
                                return;
                            }
                        }
                        else
                        {
                            // Not a WebSocket request
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = 400; // Bad Request
                            }
                            return;
                        }

                        await next();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Exception in Websocket connection: HandleAIWebSocketConnection:: message -> {mes} -> stacktrace -> {s}", ex.Message, ex.StackTrace);
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning("Exception in Websocket connection initializing: HandleAIWebSocketConnection:: message -> {mes} -> stacktrace -> {s}", ex.Message, ex.StackTrace);
            }
        }
    }
}