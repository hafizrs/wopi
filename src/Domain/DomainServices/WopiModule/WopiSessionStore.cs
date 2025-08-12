using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule
{
    public static class WopiSessionStore
    {
        private static readonly ConcurrentDictionary<string, WopiSession> _sessions = new ConcurrentDictionary<string, WopiSession>();
        private static readonly string _sessionsDir;
        private static readonly object _fileLock = new object();
        private static ILogger _logger;

        static WopiSessionStore()
        {
            // Store sessions in the same directory as files
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _sessionsDir = Path.Combine(baseDir, "temp_files", "sessions");
            if (!Directory.Exists(_sessionsDir))
            {
                Directory.CreateDirectory(_sessionsDir);
            }
        }

        public static void Initialize(ILogger logger)
        {
            _logger = logger;
            LoadSessionsFromDisk();
        }

        public static void Set(string sessionId, WopiSession session)
        {
            _sessions.AddOrUpdate(sessionId, session, (key, oldValue) => session);
            SaveSessionToDisk(sessionId, session);
        }

        public static WopiSession Get(string sessionId)
        {
            // First try to get from memory
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                _logger?.LogDebug("Retrieved session {SessionId} from memory", sessionId);
                return session;
            }

            // If not in memory, try to load from disk
            try
            {
                var filePath = GetSessionFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var diskSession = JsonConvert.DeserializeObject<WopiSession>(json);
                    
                    if (diskSession != null)
                    {
                        // Load back into memory for future fast access
                        _sessions.TryAdd(sessionId, diskSession);
                        _logger?.LogInformation("Loaded session {SessionId} from disk and cached in memory", sessionId);
                        return diskSession;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load session {SessionId} from disk", sessionId);
            }

            _logger?.LogDebug("Session {SessionId} not found in memory or disk", sessionId);
            return null;
        }

        public static bool Has(string sessionId)
        {
            return _sessions.ContainsKey(sessionId);
        }

        public static void Delete(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
            DeleteSessionFromDisk(sessionId);
        }

        public static IEnumerable<WopiSession> GetAll()
        {
            return _sessions.Values;
        }

        private static string GetSessionFilePath(string sessionId)
        {
            return Path.Combine(_sessionsDir, $"{sessionId}.json");
        }

        private static void SaveSessionToDisk(string sessionId, WopiSession session)
        {
            try
            {
                var filePath = GetSessionFilePath(sessionId);
                var json = JsonConvert.SerializeObject(session, Formatting.Indented);
                File.WriteAllText(filePath, json);
                _logger?.LogDebug("Saved session {SessionId} to disk", sessionId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save session {SessionId} to disk", sessionId);
            }
        }

        private static void DeleteSessionFromDisk(string sessionId)
        {
            try
            {
                var filePath = GetSessionFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger?.LogDebug("Deleted session file {SessionId} from disk", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete session {SessionId} from disk", sessionId);
            }
        }

        private static void LoadSessionsFromDisk()
        {
            try
            {
                if (Directory.Exists(_sessionsDir))
                {
                    var sessionFiles = Directory.GetFiles(_sessionsDir, "*.json");
                    _logger?.LogInformation("Found {Count} session files on disk", sessionFiles.Length);

                    foreach (var sessionFile in sessionFiles)
                    {
                        try
                        {
                            var json = File.ReadAllText(sessionFile);
                            var session = JsonConvert.DeserializeObject<WopiSession>(json);
                            
                            if (session != null)
                            {
                                // Verify the file still exists before loading the session
                                if (File.Exists(session.LocalFilePath))
                                {
                                    _sessions.TryAdd(session.SessionId, session);
                                    _logger?.LogDebug("Loaded session {SessionId} from disk", session.SessionId);
                                }
                                else
                                {
                                    _logger?.LogWarning("Session {SessionId} file not found, removing session file: {FilePath}", 
                                        session.SessionId, session.LocalFilePath);
                                    // Remove the orphaned session file
                                    File.Delete(sessionFile);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to load session from file: {FilePath}", sessionFile);
                            // Try to remove corrupted session file
                            try
                            {
                                File.Delete(sessionFile);
                                _logger?.LogInformation("Removed corrupted session file: {FilePath}", sessionFile);
                            }
                            catch (Exception deleteEx)
                            {
                                _logger?.LogError(deleteEx, "Failed to remove corrupted session file: {FilePath}", sessionFile);
                            }
                        }
                    }
                    
                    _logger?.LogInformation("Successfully loaded {Count} sessions from disk", _sessions.Count);
                }
                else
                {
                    _logger?.LogInformation("No sessions directory found, starting with empty store");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load sessions from disk");
            }
        }


    }
} 