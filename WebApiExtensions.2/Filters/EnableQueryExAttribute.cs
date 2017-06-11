using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.OData;
using System.Web.OData.Extensions;
using Microsoft.OData.Edm;
using System.Web.OData.Builder;
using System.Collections.Generic;

namespace WebApiExtensions.Filters
{
    public class EnableQueryExAttribute : EnableQueryAttribute
    {
        private const string ModelKeyPrefix = "MS_EdmModel";
        public static readonly List<Type> KnownComplexTypes = new List<Type>();
        public static string CountHeader = "X-Count";

        public override IEdmModel GetModel(Type elementClrType, HttpRequestMessage request, HttpActionDescriptor actionDescriptor)
        {
            return actionDescriptor.Properties.GetOrAdd(ModelKeyPrefix + elementClrType.FullName, _ =>
            {
                var builder = new ODataConventionModelBuilder(actionDescriptor.Configuration, true);
                foreach (var type in KnownComplexTypes)
                    builder.AddComplexType(type);
                var config = builder.AddEntityType(elementClrType);
                builder.AddEntitySet(elementClrType.Name, config);
                return builder.GetEdmModel();
            }) as IEdmModel;
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);

            var count = actionExecutedContext.Request.ODataProperties().TotalCount;
            if (count.HasValue)
                actionExecutedContext.Response.Headers.Add(CountHeader, count.Value.ToString());
        }
    }
}
