using AdSelfServicePortal.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdSelfServicePortal.Filters
{
    public class AdminAuthFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (session.GetString(SessionKeys.AdminUser) == null)
            {
                context.Result = new RedirectToActionResult("Login", "Admin", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
