using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models.Constants;
using Models.JsonResponse;

namespace eAGM_eRequest_API.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeTokenAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // skip authorization if action is decorated with [AllowAnonymous] attribute
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous)
                return;

            // authorization
            string? sessionid = (string?)context.HttpContext.Items["SessionID"];

            if (sessionid == null)
            {
                context.Result = new CustomErrorResult(StatusCodes.Status401Unauthorized, ResponseDesc.RES_CODE_TOKEN_EMPTY, ResponseDesc.RES_DESC_TOKEN_EMPTY);
            }
            //else
            //{
            //    context.Result = new CustomErrorResult(StatusCodes.Status403Forbidden, ResponseDesc.RES_CODE_TOKEN_NOT_PIN, ResponseDesc.RES_DESC_TOKEN_NOT_PIN);
            //}
        }

        public class CustomErrorResult : JsonResult
        {
            public CustomErrorResult(int statusCode, string code, string message = "") : base(ResponseModel<dynamic>.Error(code, message))
            {
                StatusCode = statusCode;
            }
        }
    }

}
