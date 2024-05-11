using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebAdsDemo.Extensions
{
    public static class WebApplicationExtensions
    {
        public static void ConfigErrorHandler(this WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "applicaton/problem+json";

                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    var problemDetail = new ProblemDetails
                    {
                        Title = "Internal server error!",
                        Detail = exception?.Message,
                        Instance = exceptionHandlerPathFeature?.Path,
                        Status = context.Response.StatusCode,
                    };

                    await context.Response.WriteAsJsonAsync(problemDetail);
                });
            });

        }

        public static void ConfigWebSocket(this WebApplication app)
        {
        }
    }
}