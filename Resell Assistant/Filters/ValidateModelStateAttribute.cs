using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Resell_Assistant.Models;

namespace Resell_Assistant.Filters
{
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                var errorResponse = new ErrorResponse
                {
                    StatusCode = 400,
                    Message = "Validation failed",
                    Details = string.Join("; ", errors)
                };

                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }
    }
}
