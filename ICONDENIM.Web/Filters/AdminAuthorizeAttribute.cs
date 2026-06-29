using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace ICONDENIM.Web.Filters;

public class AdminAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var adminId = context.HttpContext.Session.GetInt32("AdminUserID");
        if (!adminId.HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Admin", new { returnUrl = context.HttpContext.Request.Path.ToString() });
            return;
        }
        base.OnActionExecuting(context);
    }
}
