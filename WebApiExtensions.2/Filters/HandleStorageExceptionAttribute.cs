using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using Microsoft.WindowsAzure.Storage;

namespace WebApiExtensions.Filters
{
    public class HandleStorageExceptionAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception is StorageException ex)
            {
                var info = ex.RequestInformation.ExtendedErrorInformation;
                var error = new HttpError(info.ErrorMessage)
                {
                    {"Code", info.ErrorCode},
                    {"AdditionalInfo", info.AdditionalDetails}
                };
                actionExecutedContext.Response = actionExecutedContext.Request.CreateErrorResponse((HttpStatusCode)ex.RequestInformation.HttpStatusCode, error);
            }
        }
    }
}
