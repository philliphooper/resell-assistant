using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Resell_Assistant.Filters
{
    /// <summary>
    /// Attribute to disable TempData for specific actions (useful for SSE endpoints)
    /// </summary>
    public class DisableTempDataAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Mark this request to skip TempData processing
            context.HttpContext.Items["DisableTempData"] = true;
            base.OnActionExecuting(context);
        }
    }
}
