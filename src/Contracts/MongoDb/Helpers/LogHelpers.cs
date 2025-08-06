using System;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Helpers
{
    public class LogHelpers
    {
        protected LogHelpers() { }
        public const string ErrorLogMessageInfo = "[Log Form Selise.SC.Ecap.MongoDb lib] INFO ";
        public const string ErrorLogMessageFail = "[Log Form Selise.SC.Ecap.MongoDb lib] FAIL ";
        public const string ErrorLogMessageSuccess = "[Log Form Selise.SC.Ecap.MongoDb lib] SUCCESS ";
        public const string ErrorLogMessageException = "[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION ";
        private const string NameSpace = "Selise.Ecap.SC.Wopi.Domain.Infrastructure";
        public static readonly string LogMessageInfo = $"[Log Form {NameSpace}] INFO ";
        public static readonly string LogMessageFail = $"[Log Form {NameSpace}] FAIL ";
        public static readonly string LogMessageSuccess = $"[Log Form {NameSpace}] SUCCESS ";
        public static readonly string LogMessageException = $"[Log Form {NameSpace}] EXCEPTION ";

        public static string JsonToString(object data)
        {
            return "data -> " + JsonConvert.SerializeObject(data) + " ";
        }

        public static string JsonToString(string authData, object data)
        {
            return authData + " -> " + JsonConvert.SerializeObject(data) + " ";
        }

        public static string ExceptionToString(Exception ex)
        {
            return "Exception :: Message -> " + ex.Message + " InnerException ->" + $" {ex.InnerException} StackTrace -> {ex.StackTrace} ";
        }
    }
}
