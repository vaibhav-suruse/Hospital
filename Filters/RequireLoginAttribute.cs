using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApplicationSampleTest2.Filters
{
    // BUGFIX: this app calls app.UseAuthorization() in Startup.cs but never
    // calls app.UseAuthentication(), and no controller anywhere has an
    // [Authorize] attribute - meaning every action, including all of Daily
    // Notes, was reachable by anyone with the URL, logged in or not.
    // Real login (UserController.LoginClick) does reliably set
    // Session["UserId"], so this filter uses that as the access check.
    // This is a scoped fix for this controller, not a replacement for
    // proper ASP.NET Core authentication across the whole app.
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId <= 0)
            {
                if (IsAjaxOrJsonRequest(context.HttpContext))
                {
                    context.Result = new JsonResult(new { success = false, message = "Session expired. Please log in again." });
                }
                else
                {
                    context.Result = new RedirectToActionResult("Login", "User", null);
                }
                return;
            }
            base.OnActionExecuting(context);
        }

        private bool IsAjaxOrJsonRequest(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            return httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || (httpContext.Request.ContentType != null && httpContext.Request.ContentType.Contains("application/json"));
        }
    }
}
