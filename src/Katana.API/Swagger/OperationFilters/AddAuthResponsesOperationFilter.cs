using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Katana.API.Swagger.OperationFilters
{
    /// <summary>
    /// Adds 401/403 (and a basic 500) response documentation to endpoints protected by [Authorize].
    /// Also wires a simple JSON example for unauthorized/forbidden responses.
    /// </summary>
    public class AddAuthResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize =
                context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false);

            if (!hasAuthorize)
                return;

            // 401 Unauthorized
            if (!operation.Responses.ContainsKey("401"))
            {
                operation.Responses.Add("401", new OpenApiResponse
                {
                    Description = "Unauthorized — missing or invalid JWT token",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["message"] = new OpenApiString("Unauthorized"),
                                ["details"] = new OpenApiString("JWT token is missing or invalid"),
                                ["timestamp"] = new OpenApiString("2025-11-09T12:00:00Z")
                            }
                        }
                    }
                });
            }

            // 403 Forbidden
            if (!operation.Responses.ContainsKey("403"))
            {
                operation.Responses.Add("403", new OpenApiResponse
                {
                    Description = "Forbidden — insufficient permissions to access this resource",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["error"] = new OpenApiString("Forbidden"),
                                ["reason"] = new OpenApiString("User does not have required role(s)")
                            }
                        }
                    }
                });
            }

            // 500 Internal Server Error (basic)
            if (!operation.Responses.ContainsKey("500"))
            {
                operation.Responses.Add("500", new OpenApiResponse
                {
                    Description = "Server error",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["error"] = new OpenApiString("An unexpected error occurred")
                            }
                        }
                    }
                });
            }
        }
    }
}

