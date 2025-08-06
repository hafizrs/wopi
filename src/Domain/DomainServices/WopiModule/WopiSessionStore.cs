using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule
{
    public static class WopiSessionStore
    {
        private static readonly ConcurrentDictionary<string, WopiSession> _sessions = new ConcurrentDictionary<string, WopiSession>();

        public static void Set(string sessionId, WopiSession session)
        {
            _sessions.AddOrUpdate(sessionId, session, (key, oldValue) => session);
        }

        public static WopiSession Get(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public static bool Has(string sessionId)
        {
            return _sessions.ContainsKey(sessionId);
        }

        public static void Delete(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        public static IEnumerable<WopiSession> GetAll()
        {
            return _sessions.Values;
        }
    }
} 