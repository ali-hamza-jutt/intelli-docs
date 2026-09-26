using System.Text.Json.Serialization;
using DocuMind.Api.Extensions;
using DocuMind.Api.Middleware;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddAiServices(builder.Configuration);
builder.Services.AddBackgroundIngestion();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddTokenAuthentication(builder.Configuration);
builder.Services.AddSpaCors(builder.Configuration);

// Restricting to JSON keeps the OpenAPI document (and the generated client) free of
// text/plain and text/json duplicates of every response type.
builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new ProducesAttribute("application/json"));
        options.Filters.Add<ValidationFilter>();
    })
    // ASP.NET Core's web defaults accept numbers sent as strings ("42"), and the OpenAPI document
    // faithfully describes every integer as "integer or string". The generated client then types
    // them all as number | string, where a + b can silently concatenate. Numbers are numbers.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);

// The OpenAPI generator reads these options rather than the MVC ones, so both are set.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddConsistentValidationErrors();
builder.Services.AddEndpointRateLimits();

// The ProblemDetails writer that the exception handler, the rate limiter and every bare status
// result answer with. The enrichment is what makes those three indistinguishable to a client: a
// 404 returned by a controller carries the same code and the same readable detail as one thrown
// from a service, so nothing has to understand two shapes of the same failure.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ProblemDetailsDefaults.Apply);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// First in the pipeline, so it catches anything thrown further down and nothing internal escapes.
app.UseExceptionHandler();

app.UseRequestLogging();

if (app.Environment.IsDevelopment())
{
    // Serves the generated spec at /openapi/v1.json ...
    app.MapOpenApi();

    // ... and points Swagger UI at it. Only the UI package is referenced: AddOpenApi above
    // already builds the document, so Swashbuckle's own generator would be a second, competing pipeline.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DocuMind API v1"));
}
else
{
    // Redirecting in development would break the SPA's plain-HTTP calls to port 5100.
    app.UseHttpsRedirection();
}

app.UseCors(ServiceCollectionExtensions.SpaCorsPolicy);

// Order matters: authentication populates the principal, authorization then inspects it.
app.UseAuthentication();
app.UseAuthorization();

// After authentication: the limiter partitions by user id, which only exists once the token has
// been read.
app.UseRateLimiter();

app.MapControllers();

app.MapGet("/health", (IHealthService health) => health.GetStatus())
    .WithName("GetHealth");

app.Run();
