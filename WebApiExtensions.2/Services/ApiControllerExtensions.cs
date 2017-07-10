using System.Net.Http;
using System.Web.Http;
using DryIoc.WebApi;
using DryIoc;

namespace WebApiExtensions.Services
{
    public static class ApiControllerExtensions
    {
        public static TCtrl CreateController<TCtrl>(this ApiController mainCtrl) where TCtrl : ApiController, new()
        {
            var container = ((DryIocDependencyScope)mainCtrl.Request.GetDependencyScope()).ScopedContainer;
            var instance = (TCtrl)container.Resolve(typeof(TCtrl));
            if (instance.Request != null)
                return instance;

            instance.RequestContext = mainCtrl.RequestContext;
            instance.Request = mainCtrl.Request;
            instance.ActionContext = mainCtrl.ActionContext;
            instance.Configuration = mainCtrl.Configuration;
            instance.ControllerContext = mainCtrl.ControllerContext;
            instance.Url = mainCtrl.Url;
            instance.User = mainCtrl.User;
            return instance;
        }
    }
}