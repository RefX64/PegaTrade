using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Layer.Models;

namespace PegaTrade.Helpers
{
    public class AuthorizedUser : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            string currentActionName = (context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)?.ActionName;
            if (currentActionName == "ViewPortfolio" || currentActionName == "LoadPortfolioViewMode") { return; }

            if (context.HttpContext == null || !context.HttpContext.Session.IsAvailable ||
                string.IsNullOrEmpty(context.HttpContext.Session.GetString(Constant.Session.SessionCurrentUser)))
            {
                context.Result = context.HttpContext != null && context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" // IsAjax()
                                 ? ActionURL.RedirectTo("SessionTimeout", "Account") :
                                   new RedirectToRouteResult(new RouteValueDictionary
                                   {
                                       { "Action", "Index" },
                                       { "Controller", "Home" },
                                       { "timeout", "true" }
                                   });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    public class ValidWriteUserOnly : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            string currentActionName = (context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)?.ActionName;
            if (!string.IsNullOrEmpty(currentActionName) && currentActionName.ToUpperInvariant().StartsWith("GET")) { return; }

            string sessionStr = context.HttpContext?.Session?.GetString(Constant.Session.SessionCurrentUser);
            Layer.Models.Account.PegaUser user = string.IsNullOrEmpty(sessionStr) ? null : Utilities.Deserialize<Layer.Models.Account.PegaUser>(sessionStr);

            if (user == null || user.Username?.ToUpper() == "DEMOUSER")
            {
                context.Result = ActionURL.RedirectTo("NotValidWriteUser", "Account");
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    public class ValidateReCaptcha : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (AppSettingsProvider.IsDevelopment) { return;  }

            string recaptchaResponse = context.HttpContext.Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(recaptchaResponse) || !Utilities.ValidateReCaptcha("6LczaDgUAAAAANhIBTzGSjL11MUXQsEbb-oVFWYf", recaptchaResponse))
            {
                context.Result = new JsonResult(Layer.Models.Helpers.ResultsItem.Error("ReCapthca verification failed. Please try again."));
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    internal static class ActionURL
    {
        internal static RedirectToRouteResult RedirectTo(string action, string controller)
        {
            return new RedirectToRouteResult(new RouteValueDictionary
            {
                { "Action", action },
                { "Controller", controller }
            });
        }
    }
}
