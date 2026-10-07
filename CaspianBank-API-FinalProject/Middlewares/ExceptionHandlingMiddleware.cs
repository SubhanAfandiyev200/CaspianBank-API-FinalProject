using Service.Helpers.Exceptions;

namespace CaspianBank_API_FinalProject.Middlewares
{
    // Servislərdə atılan xətaları tutur və hamısını eyni formada qaytarır: { isSuccess: false, errors: [...] }
    // Yeni xəta növü (məsələn BadRequestException) əlavə etmək üçün yalnız yeni catch bloku yazmaq kifayətdir
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (NotFoundException ex)
            {
                await WriteAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // İstifadəçi sorğunu özü ləğv edib, cavab göndərməyin mənası yoxdur
            }
            catch (Exception ex)
            {
                // Real xəta yalnız log-a yazılır, istifadəçiyə daxili məlumat verilmir
                _logger.LogError(ex, "Unhandled exception: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status500InternalServerError, "Something went wrong. Please try again later.");
            }
        }

        private static async Task WriteAsync(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new { isSuccess = false, errors = new[] { message } });
        }
    }
}
