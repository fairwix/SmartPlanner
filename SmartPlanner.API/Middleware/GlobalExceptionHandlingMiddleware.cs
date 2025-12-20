using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartPlanner.API.Filters;

    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var problemDetails = new ProblemDetails();

            switch (exception)
            {
                case ValidationException validationEx:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Validation error";
                    problemDetails.Detail = validationEx.Message;
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                case ArgumentException argumentEx:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Bad request";
                    problemDetails.Detail = argumentEx.Message;
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                default:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Internal server error";
                    problemDetails.Detail = "An unexpected error occurred";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    break;
            }

            problemDetails.Instance = context.Request.Path;

            var json = System.Text.Json.JsonSerializer.Serialize(problemDetails);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    // Фильтр для обработки исключений
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "An unhandled exception occurred");

            var problemDetails = new ProblemDetails();

            switch (context.Exception)
            {
                case ValidationException validationEx:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Validation error";
                    problemDetails.Detail = validationEx.Message;
                    context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                case ArgumentException argumentEx:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Bad request";
                    problemDetails.Detail = argumentEx.Message;
                    context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                default:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Internal server error";
                    problemDetails.Detail = "An unexpected error occurred";
                    context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    break;
            }

            problemDetails.Instance = context.HttpContext.Request.Path;
            context.Result = new JsonResult(problemDetails) { StatusCode = context.HttpContext.Response.StatusCode };
            context.ExceptionHandled = true;
        }
    }
