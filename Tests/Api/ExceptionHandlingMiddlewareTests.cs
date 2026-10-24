using System.Text.Json;
using CaspianBank_API_FinalProject.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Service.Helpers.Exceptions;
using Xunit;

namespace CaspianBank.Tests.Api
{
    public class ExceptionHandlingMiddlewareTests
    {
        private static async Task<(DefaultHttpContext Context, JsonElement Body)> RunAsync(RequestDelegate next, CancellationToken aborted = default)
        {
            DefaultHttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestAborted = aborted;

            ExceptionHandlingMiddleware middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
            await middleware.InvokeAsync(context);

            context.Response.Body.Position = 0;
            if (context.Response.Body.Length == 0)
            {
                return (context, default);
            }

            JsonDocument doc = await JsonDocument.ParseAsync(context.Response.Body);
            return (context, doc.RootElement.Clone());
        }

        [Fact]
        public async Task No_exception_passes_through_untouched()
        {
            (DefaultHttpContext ctx, JsonElement body) = await RunAsync(c =>
            {
                c.Response.StatusCode = 204;
                return Task.CompletedTask;
            });

            Assert.Equal(204, ctx.Response.StatusCode);
            Assert.Equal(JsonValueKind.Undefined, body.ValueKind);
        }

        [Fact]
        public async Task NotFound_returns_404_with_message()
        {
            (DefaultHttpContext ctx, JsonElement body) = await RunAsync(c => throw new NotFoundException("Card not found."));

            Assert.Equal(404, ctx.Response.StatusCode);
            Assert.False(body.GetProperty("isSuccess").GetBoolean());
            Assert.Equal("Card not found.", body.GetProperty("errors")[0].GetString());
        }

        [Fact]
        public async Task BadRequest_returns_400_with_all_errors()
        {
            (DefaultHttpContext ctx, JsonElement body) = await RunAsync(c => throw new BadRequestException(new[] { "a", "b" }));

            Assert.Equal(400, ctx.Response.StatusCode);
            Assert.Equal(2, body.GetProperty("errors").GetArrayLength());
            Assert.Equal("b", body.GetProperty("errors")[1].GetString());
        }

        [Fact]
        public async Task Unknown_exception_returns_500_without_leaking_details()
        {
            (DefaultHttpContext ctx, JsonElement body) = await RunAsync(c => throw new InvalidOperationException("secret connection string"));

            Assert.Equal(500, ctx.Response.StatusCode);
            string text = body.GetRawText();
            Assert.DoesNotContain("secret", text);
            Assert.Equal("Something went wrong. Please try again later.", body.GetProperty("errors")[0].GetString());
        }

        [Fact]
        public async Task Cancelled_request_writes_nothing()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            (DefaultHttpContext ctx, JsonElement body) = await RunAsync(c => throw new OperationCanceledException(), cts.Token);

            Assert.Equal(JsonValueKind.Undefined, body.ValueKind);
            Assert.Equal(200, ctx.Response.StatusCode);
        }

        [Fact]
        public async Task Operation_cancelled_without_abort_is_a_500()
        {
            (DefaultHttpContext ctx, JsonElement body) = await RunAsync(c => throw new OperationCanceledException());

            Assert.Equal(500, ctx.Response.StatusCode);
        }

        [Fact]
        public async Task Started_response_is_not_rewritten()
        {
            DefaultHttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            FeatureStart(context);

            ExceptionHandlingMiddleware middleware = new ExceptionHandlingMiddleware(c => throw new NotFoundException("x"), NullLogger<ExceptionHandlingMiddleware>.Instance);
            await middleware.InvokeAsync(context);

            Assert.Equal(200, context.Response.StatusCode);
        }

        private static void FeatureStart(DefaultHttpContext context)
        {
            context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());
        }

        private sealed class StartedResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
        {
            public int StatusCode { get; set; } = 200;
            public string? ReasonPhrase { get; set; }
            public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
            public Stream Body { get; set; } = new MemoryStream();
            public bool HasStarted => true;
            public void OnStarting(Func<object, Task> callback, object state) { }
            public void OnCompleted(Func<object, Task> callback, object state) { }
        }
    }
}
