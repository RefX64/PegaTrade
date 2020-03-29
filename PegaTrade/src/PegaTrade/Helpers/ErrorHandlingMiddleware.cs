using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Helper;

namespace PegaTrade.Helpers
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context /* other scoped dependencies */)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode code = HttpStatusCode.InternalServerError; // 500 if unexpected
            
            if (exception is UnauthorizedAccessException) code = HttpStatusCode.Unauthorized;

            if (!AppSettingsProvider.IsDevelopment)
            {
                var _ = Utilities.LogExceptionAsync(new[] { "Middleware Exception" }, exception);
            }

            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(exception.Message);
        }
    }
}
