using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;

namespace Bitai.WebApi.Server
{
    /// <summary>
    /// Middleware to handle Web Api Exceptions
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        //private readonly ILoggerManager _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next /*, ILoggerManager logger*/)
        {
            //_logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Something went wrong: {ex}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode httpStatusCode;
            if (typeof(Server.ResourceNotFoundException).Equals(exception.GetType()))
                httpStatusCode = HttpStatusCode.NotFound;
            else
                httpStatusCode = HttpStatusCode.InternalServerError;

            context.Response.StatusCode = (int)httpStatusCode;
            context.Response.ContentType = Common.MediaTypes.ApplicationJson;

            var jsonFormat = new Server.MiddlewareException(exception);

            return context.Response.WriteAsync(JsonSerializer.Serialize(jsonFormat));
        }
    }
}