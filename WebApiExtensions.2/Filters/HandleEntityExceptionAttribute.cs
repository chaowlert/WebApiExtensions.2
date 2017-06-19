using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Filters;
using WebApiExtensions.Services;

namespace WebApiExtensions.Filters
{
    public class HandleEntityExceptionAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception is DbEntityValidationException validationEx)
            {
                var models = (from entry in validationEx.EntityValidationErrors
                    where !entry.IsValid
                    select new
                    {
                        Model = entry.Entry.Entity.GetType().Name,
                        Errors = entry.ValidationErrors
                    }).ToList();
                var error = new HttpError("Incorrect input value(s)")
                {
                    {"Code", nameof(HttpStatusCode.BadRequest)},
                    {"AdditionalInfo", new { Models = models }}
                };
                actionExecutedContext.Response = actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, error);
            }
            else if (actionExecutedContext.Exception is DbUpdateException updateEx)
            {
                var ex = updateEx.InnerException?.InnerException;
                if (ex != null)
                {
                    if (ex.Message.Contains("duplicate key"))
                    {
                        var match = Regex.Match(ex.Message, "'(?<name>(PK|IX)_[^']*)'").Groups["name"];
                        if (!match.Success || match.Value.StartsWith("PK_"))
                            actionExecutedContext.Response = actionExecutedContext.Request.CreateDuplicateErrorResponse("id");
                        else
                            actionExecutedContext.Response = actionExecutedContext.Request.CreateDuplicateErrorResponse(match.Value.Substring(3));
                    }
                    else if (ex.Message.Contains("FOREIGN KEY") || ex.Message.Contains("REFERENCE"))
                    {
                        var match = Regex.Match(ex.Message, "\"(?<name>FK_[^\"]*)\"").Groups["name"];
                        actionExecutedContext.Response = actionExecutedContext.Request.CreateReferenceErrorResponse(match.Success ? match.Value.Split('_').Last() : null);
                    }
                }
            }
            else if (actionExecutedContext.Exception is DbUpdateConcurrencyException)
            {
                var error = new HttpError("Too many requests")
                {
                    {"Code", "TooManyRequests"},
                };
                actionExecutedContext.Response = actionExecutedContext.Request.CreateErrorResponse((HttpStatusCode) 429, error);
            }
        }

    }
}
