using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace WebApiExtensions.Services
{
    public static class HttpRequestMessageExtensions
    {
        public static HttpResponseMessage CreateDuplicateErrorResponse(this HttpRequestMessage request, string paramName)
        {
            var info = new JObject
            {
                ["ParamName"] = paramName
            };
            throw request.CreateErrorCodeResponse(HttpStatusCode.Conflict, "Data is duplicated", info).ToException();
        }
        public static HttpResponseMessage CreateReferenceErrorResponse(this HttpRequestMessage request, string paramName = null)
        {
            JObject info = null;
            if (paramName != null)
            {
                info = new JObject
                {
                    ["ParamName"] = paramName
                };
            }
            throw request.CreateErrorCodeResponse(HttpStatusCode.Conflict, "Reference id is invalid", info).ToException();
        }
    }
}
