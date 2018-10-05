using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Http;
using System.Web.Http.Batch;
using EF6.Extensions;
using JsonNet.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApiExtensions.Handlers
{
    public class JsonBatchHandler : HttpBatchHandler
    {
        static readonly Regex regex = new Regex("{{(.+?)}}", RegexOptions.Compiled);

        public IList<string> SupportedContentTypes { get; } = new List<string>();
        public JsonBatchHandler(HttpServer httpServer) : base(httpServer)
        {
            SupportedContentTypes.Add("application/json");
            SupportedContentTypes.Add("text/json");
        }

        public virtual void ValidateRequest(HttpRequestMessage request)
        {
            if (request.Content == null)
            {
                throw new HttpResponseException(request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Content is missing"));
            }

            var contentType = request.Content.Headers.ContentType;
            if (contentType == null)
            {
                throw new HttpResponseException(request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Content type is missing"));
            }

            if (!SupportedContentTypes.Contains(contentType.MediaType, StringComparer.OrdinalIgnoreCase))
            {
                throw new HttpResponseException(request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    contentType.MediaType + " is not supported"));
            }
        }

        public override async Task<HttpResponseMessage> ProcessBatchAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            IList<JsonRequestMessage> jsonRequests;

            try
            {
                jsonRequests = await SplitRequests(request);
            }
            catch
            {
                throw new HttpResponseException(request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Content is invalid"));
            }

            var requests = new List<HttpRequestMessage>();
            var responses = new List<HttpResponseMessage>();
            var jsonResponses = new JArray();
            TransactionScope tx = null;

            var queries = request.GetQueryNameValuePairs();
            if (queries.Any(kvp => kvp.Key == "transaction" && kvp.Value == "true"))
            {
                AzureConfiguration.SuspendExecutionStrategy = true;
                tx = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
            }

            try
            {
                var success = true;
                foreach (var jsonRequest in jsonRequests)
                {
                    var subRequest = ParseRequest(request, jsonRequest, jsonResponses);
                    requests.Add(subRequest);
                    var response = await Invoker.SendAsync(subRequest, cancellationToken);
                    responses.Add(response);
                    var jsonResponse = await ToJsonResponse(response);
                    jsonResponses.Add(jsonResponse.ToJObject());

                    if (!response.IsSuccessStatusCode)
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                    tx?.Complete();
                return request.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, jsonResponses);
            }
            catch
            {
                foreach (var response in responses)
                {
                    response?.Dispose();
                }
                throw;
            }
            finally
            {
                foreach (var subRequest in requests)
                {
                    request.RegisterForDispose(subRequest.GetResourcesForDisposal());
                    request.RegisterForDispose(subRequest);
                }
                tx?.Dispose();
                AzureConfiguration.SuspendExecutionStrategy = false;
            }
        }

        static async Task<IList<JsonRequestMessage>> SplitRequests(HttpRequestMessage request)
        {
            var jsonSubRequestsString = await request.Content.ReadAsStringAsync();
            var jsonSubRequests = JsonConvert.DeserializeObject<IList<JsonRequestMessage>>(jsonSubRequestsString);
            return jsonSubRequests;
        }

        static HttpRequestMessage ParseRequest(HttpRequestMessage request, JsonRequestMessage jsonRequest, JToken jsonResponse)
        {
            var urlStr = $"{request.RequestUri.Scheme}://{request.RequestUri.Host}{(request.RequestUri.IsDefaultPort ? "" : ":" + request.RequestUri.Port)}{jsonRequest.url}";
            urlStr = ReplaceJsonPath(urlStr, jsonResponse);
            var subRequestUri = new Uri(urlStr);

            var rm = new HttpRequestMessage(new HttpMethod(jsonRequest.type), subRequestUri);
            foreach (var item in request.Headers)
            {
                rm.Headers.Add(item.Key, item.Value);
            }

            if (jsonRequest.data != null)
            {
                var data = ReplaceJsonPath(jsonRequest.data, jsonResponse);
                rm.Content = new StringContent(data, Encoding.UTF8, "application/json");
            }

            rm.CopyBatchRequestProperties(request);

            return rm;
        }

        static string ReplaceJsonPath(string data, JToken jsonResponse)
        {
            return regex.Replace(data, match => jsonResponse.SelectToken(match.Groups[1].Value).ToString());
        }

        static async Task<JsonResponseMessage> ToJsonResponse(HttpResponseMessage subResponse)
        {
            var jsonResponse = new JsonResponseMessage
            {
                code = (int)subResponse.StatusCode
            };
            foreach (var header in subResponse.Headers)
            {
                jsonResponse.headers.Add(header.Key, string.Join(",", header.Value));
            }
            if (subResponse.Content != null)
            {
                jsonResponse.data = await subResponse.Content.ReadAsAsync<JToken>();
                foreach (var header in subResponse.Content.Headers)
                {
                    jsonResponse.headers.Add(header.Key, string.Join(",", header.Value));
                }
            }
            return jsonResponse;
        }
    }
    
    public class JsonResponseMessage
    {
        public JsonResponseMessage()
        {
            headers = new Dictionary<string, string>();
        }

        public int code { get; set; }

        public Dictionary<string, string> headers { get; set; }

        public JToken data { get; set; }
    }

    public class JsonRequestMessage
    {
        public string type { get; set; }

        public string url { get; set; }
        
        [JsonConverter(typeof(PlainJsonConverter))]
        public string data { get; set; }
    }
}
