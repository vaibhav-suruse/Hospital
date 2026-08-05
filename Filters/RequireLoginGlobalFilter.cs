using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace WebApplicationSampleTest2.Filters
{
    // ── GLOBAL AUTHENTICATION FILTER ─────────────────────────────────────
    // Previously only DailyNotesController carried a [RequireLogin] attribute,
    // so every other controller (patients, IPD admissions, billing, invoices,
    // documents, user management, etc.) was reachable by anyone who knew the
    // URL — logged in or not.
    //
    // This filter is registered globally in Startup.ConfigureServices so it
    // runs for EVERY controller/action. It requires Session["UserId"] (set by
    // UserController.LoginClick) and permanently redirects unauthenticated
    // requests to the login page. A small allowlist keeps genuinely public
    // endpoints (login, registration, patient portal, home) accessible.
    //
    // NOTE: This is a much-needed scoped hardening, not a replacement for
    // proper ASP.NET Core authentication (there is still no
    // app.UseAuthentication() / [Authorize]). It closes the "anyone with the
    // URL can read billing/patient data" gap using the same session primitive
    // the rest of this app already relies on.
    public class RequireLoginGlobalFilter : ActionFilterAttribute
    {
// Controllers that are fully public (login/registration/home/portal).
        private static readonly string[] AnonymousControllers =
        {
            "Login",
            "Home",
            // PatientPortal authenticates with its OWN session keys
            // (AccountId / PatientId), NOT the staff UserId this filter
            // checks. Every PatientPortal action already enforces
            // RedirectIfNotLoggedIn() / AccountId internally, so the whole
            // controller must be exempt from this staff-focused filter to
            // avoid locking legitimate patient users out.
            "PatientPortal"
        };

        // Controller:Action pairs that must stay public even though the rest
        // of the controller is authenticated (e.g. pre-login AJAX on the
        // login page itself).
private static readonly (string Controller, string Action)[] AnonymousEndpoints =
        {
            ("User", "Login"),
            // The login POST runs BEFORE Session["UserId"] is created, so it
            // must be allowed through the filter or no one can ever log in.
            ("User", "LoginClick"),
            ("User", "IdentifyUser"),
            ("User", "GetSubHospitals"),
        };

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString() ?? "";

            // Always allow public controllers wholesale.
            if (AnonymousControllers.Contains(controller, StringComparer.OrdinalIgnoreCase))
            {
                base.OnActionExecuting(context);
                return;
            }

            // Allow specific public endpoints on otherwise-protected controllers.
            if (AnonymousEndpoints.Any(e =>
                    string.Equals(e.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.Action, action, StringComparison.OrdinalIgnoreCase)))
            {
                base.OnActionExecuting(context);
                return;
            }

            // Everything else requires a signed-in user.
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

        private bool IsAjaxOrJsonRequest(HttpContext httpContext)
        {
            return httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || (httpContext.Request.ContentType != null && httpContext.Request.ContentType.Contains("application/json"));
        }
    }
}
