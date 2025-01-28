using Microsoft.AspNetCore.Mvc.Filters;

namespace GatewayService.Filters
{
    public class CustomHeaderFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {

        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Response.Headers.Append("X-Developer-Name", "YourName");
        }
    }
}
