using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CustomLogger;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class AppLogRecordedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<AppLogRecordedEventHandler> _logger;
        private readonly IRepository _repository;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly List<ItemIdAndTitle> _logEmailList;
        private readonly string _env;
        private readonly string _teamsWebhookUrl;
        private static IDictionary<string, (int, DateTime)> exceptionCacheDict = new Dictionary<string, (int, DateTime)>();
        private static IDictionary<string, (int, DateTime)> messageCacheDict = new Dictionary<string, (int, DateTime)>();
        private static DateTime resetDate = DateTime.UtcNow.Date;
        private readonly bool _isEnabledLogRecord;


        public AppLogRecordedEventHandler(
            ILogger<AppLogRecordedEventHandler> logger,
            IRepository repository,
            IEmailNotifierService emailNotifierService,
            IConfiguration configuration)
        {
            _logger = logger;
            _repository = repository;
            _emailNotifierService = emailNotifierService;
            _logEmailList = configuration.GetSection("LogEmailList")
                            .GetChildren()
                            .Select(itemSection => new ItemIdAndTitle
                            {
                                ItemId = itemSection.GetSection("ItemId").Value,
                                Title = itemSection.GetSection("Title").Value
                            })
                            .ToList();
            _env = configuration["Environment"];
            _teamsWebhookUrl = configuration["TeamsWebhookUrl"];
            _isEnabledLogRecord = configuration["EnableLogRecord"] == "1";
        }

        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.", nameof(AppLogRecordedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                if (_env == "Local" || !_isEnabledLogRecord) return false;

                var logRecord = JsonConvert.DeserializeObject<AppLogRecord>(@event.JsonPayload);
                ResetDict();

                if (logRecord != null)
                {
                    if (logRecord.ExceptionMessage != null)
                    {
                        var hashKey = GenerateHashKey(logRecord.Source + logRecord.ExceptionMessage + logRecord.StackTrace);
                        if (exceptionCacheDict.ContainsKey(hashKey))
                        {
                            exceptionCacheDict[hashKey] = (exceptionCacheDict[hashKey].Item1 + 1, DateTime.UtcNow);
                            return true;
                        }
                        exceptionCacheDict.Add(hashKey, (1, DateTime.UtcNow));
                    }
                    else
                    {
                        var hashKey = GenerateHashKey(logRecord.Source + logRecord.Message);
                        if (messageCacheDict.ContainsKey(hashKey))
                        {
                            messageCacheDict[hashKey] =  (messageCacheDict[hashKey].Item1 + 1, DateTime.UtcNow);
                            return true;
                        }
                        messageCacheDict.Add(hashKey, (1, DateTime.UtcNow));
                    }
                    await SendLogToTeamChannelViaAdaptiveCard(logRecord);
                }
                else 
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.AppLogRecordedEvent));
                _logger.LogWarning("Exception Message: {Message} \nException Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation($"Handled by: {nameof(AppLogRecordedEventHandler)}.");

            return response;
        }

        private static string GenerateHashKey(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                text = Regex.Replace(text.Trim(), @"\s+", " ");
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha256.ComputeHash(textBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private async Task SendLogToTeamChannelViaAdaptiveCard(AppLogRecord logRecord)
        {
            var isProd = _env == "Production";
            var isStg = _env == "Stage";

            string mentionNames = isProd
                ? (string.Join(", ", _logEmailList.Select(u => $"<at>{u.Title}</at>")) + " 🚨 **PRODUCTION ALERT**")
                : isStg
                    ? (string.Join(", ", _logEmailList.Select(u => $"<at>{u.Title}</at>")) + " 🔶 **[STAGING ALERT]**")
                    : null;

            var body = new JArray();

            if (!string.IsNullOrEmpty(mentionNames))
            {
                body.Add(new JObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = $"{mentionNames}",
                    ["wrap"] = true,
                    ["weight"] = "Bolder",
                    ["color"] = "Attention",
                    ["size"] = "Medium"
                });
            }

            if (!isProd && !isStg) body.Add(new JObject { ["type"] = "TextBlock", ["text"] = $"**Environment:** {_env}", ["wrap"] = true });
            //body.Add(new JObject { ["type"] = "TextBlock", ["text"] = $"**Log Type:** {logRecord.LogLevel}", ["wrap"] = true });
            body.Add(new JObject { ["type"] = "TextBlock", ["text"] = $"**CreateTime:** {logRecord.CreateDate}", ["wrap"] = true });
            body.Add(new JObject { ["type"] = "TextBlock", ["text"] = $"**Source:** {logRecord.Source}", ["wrap"] = true });
            body.Add(new JObject { ["type"] = "TextBlock", ["text"] = $"**Message:** {logRecord.Message}", ["wrap"] = true });
            body.Add(new JObject { ["type"] = "TextBlock", ["text"] = $"**Exception Message:** {logRecord.ExceptionMessage}", ["wrap"] = true });
            body.Add(new JObject { ["type"] = "TextBlock", ["text"] = "**StackTrace:**", ["weight"] = "Bolder" });
            body.Add(new JObject { ["type"] = "TextBlock", ["text"] = logRecord.StackTrace, ["wrap"] = true });

            var cardContent = new JObject
            {
                ["type"] = "AdaptiveCard",
                ["version"] = "1.4",
                ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                ["body"] = body,
                ["msteams"] = new JObject
                {
                    ["width"] = "Full"
                }
            };

            if (isProd || isStg)
            {
                var mentionEntities = new JArray(
                    _logEmailList.Select(u =>
                        new JObject
                        {
                            ["type"] = "mention",
                            ["text"] = $"<at>{u.Title}</at>",
                            ["mentioned"] = new JObject
                            {
                                ["id"] = u.ItemId,
                                ["name"] = u.Title
                            }
                        })
                );

                cardContent["msteams"]["entities"] = mentionEntities;
            }

            var payload = new JObject
            {
                ["type"] = "message",
                ["attachments"] = new JArray
                {
                    new JObject
                    {
                        ["contentType"] = "application/vnd.microsoft.card.adaptive",
                        ["content"] = cardContent
                    }
                }
            };

            using var client = new HttpClient();
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_teamsWebhookUrl, content);
            response.EnsureSuccessStatusCode();
        }

        private void ResetDict()
        {
            try
            {
                var clearDate = DateTime.UtcNow.Date.AddDays(-2);
                if (resetDate > clearDate) return;
                resetDate = DateTime.UtcNow.Date;
                var exceptions = exceptionCacheDict.ToList();
                var messages = messageCacheDict.ToList();

                foreach (var exception in exceptions)
                {
                    if (exception.Value.Item2 <= clearDate || exception.Value.Item1 >= 50)
                    {
                        exceptionCacheDict.Remove(exception.Key);
                    }
                }

                foreach (var message in messages)
                {
                    if (message.Value.Item2 <= clearDate || message.Value.Item1 >= 50)
                    {
                        messageCacheDict.Remove(message.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Exception in ResetDict, Message: {Message} \nException Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

    }
}
